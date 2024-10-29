// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Bytes.Converters.General
{
    internal sealed partial class ByteConverterObject<TParent, TValue>
    {
        private readonly struct MemberKey
        {
            public readonly ushort Index;
            public readonly ByteConverterObjectMember Member;
            public readonly ulong HashCode;

            public MemberKey(ushort index, ByteConverterObjectMember member)
            {
                this.Index = index;
                this.Member = member;
                this.HashCode = GetHashCode(member.NameAsBytes);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong GetHashCode(ReadOnlySpan<byte> name)
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
            public static bool IsEqual(in MemberKey memberKey, in ReadOnlySpan<byte> name)
            {
                var key = GetHashCode(name);
                if (key != memberKey.HashCode)
                    return false;

                if (name.Length <= 7)
                    return true;
                
                return name.SequenceEqual(memberKey.Member.NameAsBytes);
            }
        }
    }
}