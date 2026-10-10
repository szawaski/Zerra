// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NETSTANDARD2_0
using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    //RSA keys as PEM for .NET Standard, which has no PEM or ASN.1 support, tested by Zerra.Test.NetStandard
    //  PUBLIC KEY: SubjectPublicKeyInfo SEQUENCE { SEQUENCE { OID rsaEncryption, NULL }, BIT STRING { RSAPublicKey } }
    //  PRIVATE KEY: PKCS#8 SEQUENCE { INTEGER 0, SEQUENCE { OID rsaEncryption, NULL }, OCTET STRING { RSAPrivateKey } }
    //  RSA PUBLIC KEY: PKCS#1 RSAPublicKey SEQUENCE { n, e }
    //  RSA PRIVATE KEY: PKCS#1 RSAPrivateKey SEQUENCE { 0, n, e, d, p, q, dp, dq, qi }
    internal static class RsaPem
    {
        private const byte sequenceTag = 0x30;
        private const byte integerTag = 0x02;
        private const byte bitStringTag = 0x03;
        private const byte octetStringTag = 0x04;
        private const byte nullTag = 0x05;
        private const byte oidTag = 0x06;
        private static readonly byte[] rsaEncryptionOid = [0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01]; //1.2.840.113549.1.1.1

        public static string ExportPublicKey(RSAParameters parameters)
        {
            var rsaPublicKey = Encode(sequenceTag, Integer(parameters.Modulus!), Integer(parameters.Exponent!));
            var bitString = new byte[rsaPublicKey.Length + 1]; //no unused bits
            Buffer.BlockCopy(rsaPublicKey, 0, bitString, 1, rsaPublicKey.Length);
            return Pem("PUBLIC KEY", Encode(sequenceTag, AlgorithmIdentifier(), Encode(bitStringTag, bitString)));
        }

        public static string ExportPrivateKey(RSAParameters parameters)
        {
            var rsaPrivateKey = Encode(sequenceTag,
                Integer([0]),
                Integer(parameters.Modulus!), Integer(parameters.Exponent!), Integer(parameters.D!),
                Integer(parameters.P!), Integer(parameters.Q!),
                Integer(parameters.DP!), Integer(parameters.DQ!), Integer(parameters.InverseQ!));
            return Pem("PRIVATE KEY", Encode(sequenceTag, Integer([0]), AlgorithmIdentifier(), Encode(octetStringTag, rsaPrivateKey)));
        }

        public static RSAParameters Import(string pem)
        {
            if (pem is null)
                throw new ArgumentNullException(nameof(pem));

            var begin = pem.IndexOf("-----BEGIN ", StringComparison.Ordinal);
            if (begin < 0)
                throw new CryptographicException("No PEM key found");
            var labelEnd = pem.IndexOf("-----", begin + 11, StringComparison.Ordinal);
            if (labelEnd < 0)
                throw new CryptographicException("No PEM key found");
            var label = pem.Substring(begin + 11, labelEnd - begin - 11);
            var footer = $"-----END {label}-----";
            var end = pem.IndexOf(footer, labelEnd, StringComparison.Ordinal);
            if (end < 0)
                throw new CryptographicException("No PEM key found");

            byte[] der;
            try
            {
                var body = new StringBuilder(end - labelEnd);
                foreach (var c in pem.Substring(labelEnd + 5, end - labelEnd - 5))
                {
                    if (!Char.IsWhiteSpace(c))
                        _ = body.Append(c);
                }
                der = Convert.FromBase64String(body.ToString());
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Invalid PEM key", ex);
            }

            var reader = new DerReader(der, 0, der.Length);
            RSAParameters parameters;
            switch (label)
            {
                case "PUBLIC KEY":
                    {
                        var spki = reader.Read(sequenceTag);
                        ReadAlgorithmIdentifier(ref spki);
                        var bitString = spki.Read(bitStringTag);
                        if (bitString.Length < 1 || bitString.ReadByte() != 0)
                            throw new CryptographicException("Invalid PEM key");
                        parameters = ReadPublicKey(bitString.Read(sequenceTag));
                        break;
                    }
                case "RSA PUBLIC KEY":
                    parameters = ReadPublicKey(reader.Read(sequenceTag));
                    break;
                case "PRIVATE KEY":
                    {
                        var pkcs8 = reader.Read(sequenceTag);
                        _ = pkcs8.Read(integerTag);
                        ReadAlgorithmIdentifier(ref pkcs8);
                        var octetString = pkcs8.Read(octetStringTag);
                        parameters = ReadPrivateKey(octetString.Read(sequenceTag));
                        break;
                    }
                case "RSA PRIVATE KEY":
                    parameters = ReadPrivateKey(reader.Read(sequenceTag));
                    break;
                default:
                    throw new CryptographicException($"PEM {label} is not supported, only RSA keys that aren't encrypted");
            }
            return parameters;
        }

        private static void ReadAlgorithmIdentifier(ref DerReader reader)
        {
            var algorithm = reader.Read(sequenceTag);
            var oid = algorithm.Read(oidTag);
            if (!oid.Equals(rsaEncryptionOid))
                throw new CryptographicException("Only RSA keys are supported");
        }

        private static RSAParameters ReadPublicKey(DerReader sequence)
        {
            return new RSAParameters()
            {
                Modulus = sequence.Read(integerTag).ToUnsigned(0),
                Exponent = sequence.Read(integerTag).ToUnsigned(0),
            };
        }

        //the BCL, .NET Framework especially, needs d the size of n and the prime values half of it
        private static RSAParameters ReadPrivateKey(DerReader sequence)
        {
            _ = sequence.Read(integerTag);
            var modulus = sequence.Read(integerTag).ToUnsigned(0);
            var half = (modulus.Length + 1) / 2;
            return new RSAParameters()
            {
                Modulus = modulus,
                Exponent = sequence.Read(integerTag).ToUnsigned(0),
                D = sequence.Read(integerTag).ToUnsigned(modulus.Length),
                P = sequence.Read(integerTag).ToUnsigned(half),
                Q = sequence.Read(integerTag).ToUnsigned(half),
                DP = sequence.Read(integerTag).ToUnsigned(half),
                DQ = sequence.Read(integerTag).ToUnsigned(half),
                InverseQ = sequence.Read(integerTag).ToUnsigned(half),
            };
        }

        private static byte[] AlgorithmIdentifier() => Encode(sequenceTag, Encode(oidTag, rsaEncryptionOid), Encode(nullTag, []));

        //an unsigned big endian value as a DER INTEGER: no leading zeros, and a zero added when the high bit is set so it isn't negative
        private static byte[] Integer(byte[] value)
        {
            var start = 0;
            while (start < value.Length - 1 && value[start] == 0)
                start++;
            var pad = (value[start] & 0x80) != 0 ? 1 : 0;
            var content = new byte[value.Length - start + pad];
            Buffer.BlockCopy(value, start, content, pad, value.Length - start);
            return Encode(integerTag, content);
        }

        private static byte[] Encode(byte tag, params byte[][] contents)
        {
            var length = 0;
            foreach (var content in contents)
                length += content.Length;

            var lengthBytes = length < 0x80 ? 1 : length <= 0xFF ? 2 : length <= 0xFFFF ? 3 : 4;
            var result = new byte[1 + lengthBytes + length];
            result[0] = tag;
            if (lengthBytes == 1)
            {
                result[1] = (byte)length;
            }
            else
            {
                result[1] = (byte)(0x80 | (lengthBytes - 1));
                for (var i = 0; i < lengthBytes - 1; i++)
                    result[1 + lengthBytes - 1 - i] = (byte)(length >> (8 * i));
            }
            var offset = 1 + lengthBytes;
            foreach (var content in contents)
            {
                Buffer.BlockCopy(content, 0, result, offset, content.Length);
                offset += content.Length;
            }
            return result;
        }

        private static string Pem(string label, byte[] der)
        {
            var base64 = Convert.ToBase64String(der);
            var builder = new StringBuilder();
            _ = builder.Append("-----BEGIN ").Append(label).Append("-----\n");
            for (var i = 0; i < base64.Length; i += 64)
                _ = builder.Append(base64, i, Math.Min(64, base64.Length - i)).Append('\n');
            _ = builder.Append("-----END ").Append(label).Append("-----");
            return builder.ToString();
        }

        private struct DerReader
        {
            private readonly byte[] data;
            private int position;
            private readonly int end;

            public DerReader(byte[] data, int offset, int length)
            {
                this.data = data;
                this.position = offset;
                this.end = offset + length;
            }

            public readonly int Length => end - position;

            public byte ReadByte()
            {
                if (position >= end)
                    throw new CryptographicException("Invalid PEM key");
                return data[position++];
            }

            public DerReader Read(byte tag)
            {
                if (ReadByte() != tag)
                    throw new CryptographicException("Invalid PEM key");
                int length = ReadByte();
                if ((length & 0x80) != 0)
                {
                    var count = length & 0x7F;
                    if (count < 1 || count > 4)
                        throw new CryptographicException("Invalid PEM key");
                    length = 0;
                    for (var i = 0; i < count; i++)
                        length = (length << 8) | ReadByte();
                }
                if (length < 0 || length > end - position)
                    throw new CryptographicException("Invalid PEM key");
                var content = new DerReader(data, position, length);
                position += length;
                return content;
            }

            public readonly bool Equals(byte[] value)
            {
                if (Length != value.Length)
                    return false;
                for (var i = 0; i < value.Length; i++)
                {
                    if (data[position + i] != value[i])
                        return false;
                }
                return true;
            }

            //the integer without its sign byte, left padded with zeros to the size given
            public readonly byte[] ToUnsigned(int size)
            {
                var start = position;
                while (start < end - 1 && data[start] == 0)
                    start++;
                var length = end - start;
                var result = new byte[Math.Max(size, length)];
                Buffer.BlockCopy(data, start, result, result.Length - length, length);
                return result;
            }
        }
    }
}
#endif
