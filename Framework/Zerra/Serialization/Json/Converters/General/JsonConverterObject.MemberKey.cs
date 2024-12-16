// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TParent, TValue>
    {
        private readonly struct MemberKey
        {
            public readonly ulong BytesHashCode;
            public readonly ulong CharsHashCode;
            public readonly JsonConverterObjectMember Member;

            public MemberKey(JsonConverterObjectMember member)
            {
                this.BytesHashCode = GetHashCode(member.JsonNameBytes);
                this.CharsHashCode = GetHashCode(member.JsonNameChars);
                this.Member = member;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ulong GetHashCode(ReadOnlySpan<byte> name)
            {
                ref byte reference = ref MemoryMarshal.GetReference(name);
                var length = name.Length;

                ulong code;
                switch (length)
                {
                    case > 7:
                        // 00000000_########_########_########_########_########_########_########
                        code = Unsafe.ReadUnaligned<ulong>(ref reference) & 0b_00000000_11111111_11111111_11111111_11111111_11111111_11111111_11111111;

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 7:
                        // 00000000_00000000_00000000_00000000_########_########_########_######## | 00000000_00000000_########_########_00000000_00000000_00000000_00000000 | 00000000_########_00000000_00000000_00000000_00000000_00000000_00000000
                        code = Unsafe.ReadUnaligned<uint>(ref reference) | ((ulong)Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref reference, 4)) << 32) | ((ulong)Unsafe.Add(ref reference, 6) << 56);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 6:
                        // 00000000_00000000_00000000_00000000_########_########_########_######## | 00000000_00000000_########_########_00000000_00000000_00000000_00000000
                        code = Unsafe.ReadUnaligned<uint>(ref reference) | ((ulong)Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref reference, 4)) << 32);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 5:
                        // 00000000_00000000_00000000_00000000_########_########_########_######## | 00000000_00000000_00000000_########_00000000_00000000_00000000_00000000
                        code = Unsafe.ReadUnaligned<uint>(ref reference) | ((ulong)Unsafe.Add(ref reference, 4) << 32);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 4:
                        // 00000000_00000000_00000000_00000000_########_########_########_########
                        code = Unsafe.ReadUnaligned<uint>(ref reference);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 3:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_########_######## | 00000000_00000000_00000000_00000000_00000000_########_00000000_00000000
                        code = Unsafe.ReadUnaligned<ushort>(ref reference) | ((ulong)Unsafe.Add(ref reference, 2) << 16);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 2:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_########_########
                        code = Unsafe.ReadUnaligned<ushort>(ref reference);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 1:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_########
                        code = Unsafe.ReadUnaligned<byte>(ref reference);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    default:
                        code = 0UL;
                        return code;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ulong GetHashCode(ReadOnlySpan<char> name)
            {
                ref byte reference = ref MemoryMarshal.GetReference(MemoryMarshal.AsBytes(name));
                var length = name.Length * 2;

                ulong code;
                //even cases not possible, common chars have 2nd byte as 0 so skip those
                switch (length)
                {
                    case > 14:
                        //take 4 bytes from front and 3 bytes from end
                        // 00000000_########_########_########_########_########_########_########
                        code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24) | ((ulong)Unsafe.Add(ref reference, length - 6) << 32) | ((ulong)Unsafe.Add(ref reference, length - 4) << 40) | ((ulong)Unsafe.Add(ref reference, length - 2) << 48);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 14:
                        // 00000000_########_########_########_########_########_########_########
                        code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24) | ((ulong)Unsafe.Add(ref reference, 8) << 32) | ((ulong)Unsafe.Add(ref reference, 10) << 40) | ((ulong)Unsafe.Add(ref reference, 12) << 48);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 12:
                        // 00000000_00000000_########_########_########_########_########_########
                        code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24) | ((ulong)Unsafe.Add(ref reference, 8) << 32) | ((ulong)Unsafe.Add(ref reference, 10) << 40);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 10:
                        // 00000000_00000000_00000000_########_########_########_########_########
                        code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24) | ((ulong)Unsafe.Add(ref reference, 8) << 32);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 8:
                        // 00000000_00000000_00000000_00000000_########_########_########_########
                        code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 6:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_######## | 00000000_00000000_00000000_00000000_00000000_00000000_########_00000000 | 00000000_00000000_00000000_00000000_00000000_########_00000000_00000000
                        code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 4:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_######## | 00000000_00000000_00000000_00000000_00000000_00000000_########_00000000
                        code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 2:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_########
                        code = (ulong)reference;

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    default:
                        code = 0UL;
                        return code;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsEqual(in MemberKey memberKey, in ReadOnlySpan<byte> name, in ulong key)
            {
                if (key != memberKey.BytesHashCode)
                    return false;

                if (name.Length <= 7)
                    return true;

                return name.SequenceEqual(memberKey.Member.JsonNameBytes);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsEqual(in MemberKey memberKey, in ReadOnlySpan<char> name, in ulong key)
            {
                if (key != memberKey.CharsHashCode)
                    return false;

                if (name.Length <= 7)
                    return true;

                return name.SequenceEqual(memberKey.Member.JsonNameChars);
            }
        }
    }
}