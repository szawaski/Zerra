﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class ByteSerializer
    {
        public static T DeserializeStackBased<T>(byte[] bytes, ByteSerializerOptions options = null)
        {
            return (T)DeserializeStackBased(typeof(T), bytes, options);
        }
        public static object DeserializeStackBased(Type type, byte[] bytes, ByteSerializerOptions options = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            options ??= defaultOptions;

            var typeDetail = GetTypeInformation(type, options.IndexSize, options.IgnoreIndexAttribute);

            var state = new ReadState()
            {
                UsePropertyNames = options.UsePropertyNames,
                IncludePropertyTypes = options.IncludePropertyTypes,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSize = options.IndexSize
            };
            state.CurrentFrame = ReadFrameFromType(ref state, typeDetail, false, true);

            Read(bytes, ref state, options.Encoding);

            if (!state.Ended || state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return state.LastFrameResultObject;
        }

        public static T Deserialize<T>(Stream stream, ByteSerializerOptions options = null)
        {
            return (T)Deserialize(typeof(T), stream, options);
        }
        public static object Deserialize(Type type, Stream stream, ByteSerializerOptions options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = GetTypeInformation(type, options.IndexSize, options.IgnoreIndexAttribute);

            var isFinalBlock = false;
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var position = 0;
                var length = 0;
                var read = -1;
                while (position < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = stream.Read(buffer, position, buffer.Length - position);
#else
                    read = stream.Read(buffer.AsSpan(position));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    position += read;
                    length = position;
                }

                if (position == 0)
                {
                    return default;
                }

                var state = new ReadState()
                {
                    UsePropertyNames = options.UsePropertyNames,
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSize = options.IndexSize
                };
                state.CurrentFrame = ReadFrameFromType(ref state, typeDetail, false, true);

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, read), ref state, options.Encoding);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (isFinalBlock)
                            throw new EndOfStreamException();

                        Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                        position = length - position;

                        if (position + state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, position + state.BytesNeeded);

                        while (position < buffer.Length)
                        {
#if NETSTANDARD2_0
                            read = stream.Read(buffer, position, buffer.Length - position);
#else
                            read = stream.Read(buffer.AsSpan(position));
#endif

                            if (read == 0)
                            {
                                isFinalBlock = true;
                                break;
                            }
                            position += read;
                            length = position;
                        }

                        if (position < state.BytesNeeded)
                            throw new EndOfStreamException();

                        state.BytesNeeded = 0;
                    }
                }

                return state.LastFrameResultObject;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        public static async Task<T> DeserializeAsync<T>(Stream stream, ByteSerializerOptions options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var type = typeof(T);

            var typeDetail = GetTypeInformation(type, options.IndexSize, options.IgnoreIndexAttribute);

            var isFinalBlock = false;
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var position = 0;
                var length = 0;
                var read = -1;
                while (position < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(position));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    position += read;
                    length = position;
                }

                if (position == 0)
                {
                    return default;
                }

                var state = new ReadState()
                {
                    UsePropertyNames = options.UsePropertyNames,
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSize = options.IndexSize
                };
                state.CurrentFrame = ReadFrameFromType(ref state, typeDetail, false, true);

                for (; ; )
                {
                    var usedBytes = Read(buffer.AsSpan().Slice(0, length), ref state, options.Encoding);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (isFinalBlock)
                            throw new EndOfStreamException();

                        Buffer.BlockCopy(buffer, usedBytes, buffer, 0, length - usedBytes);
                        position = length - usedBytes;

                        if (position + state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, position + state.BytesNeeded);

                        while (position < buffer.Length)
                        {
#if NETSTANDARD2_0
                            read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                            read = await stream.ReadAsync(buffer.AsMemory(position));
#endif

                            if (read == 0)
                            {
                                isFinalBlock = true;
                                break;
                            }
                            position += read;
                            length = position;
                        }

                        if (position < state.BytesNeeded)
                            throw new EndOfStreamException();

                        state.BytesNeeded = 0;
                    }
                }

                return (T)state.LastFrameResultObject;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }
        public static async Task<object> DeserializeAsync(Type type, Stream stream, ByteSerializerOptions options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = GetTypeInformation(type, options.IndexSize, options.IgnoreIndexAttribute);

            var isFinalBlock = false;
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var position = 0;
                var length = 0;
                var read = -1;
                while (position < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(position));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    position += read;
                    length = position;
                }

                if (position == 0)
                {
                    return default;
                }

                var state = new ReadState()
                {
                    UsePropertyNames = options.UsePropertyNames,
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSize = options.IndexSize
                };
                state.CurrentFrame = ReadFrameFromType(ref state, typeDetail, false, true);

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, read), ref state, options.Encoding);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (isFinalBlock)
                            throw new EndOfStreamException();

                        Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                        position = length - position;

                        if (position + state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, position + state.BytesNeeded);

                        while (position < buffer.Length)
                        {
#if NETSTANDARD2_0
                            read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                            read = await stream.ReadAsync(buffer.AsMemory(position));
#endif

                            if (read == 0)
                            {
                                isFinalBlock = true;
                                break;
                            }
                            position += read;
                            length = position;
                        }

                        if (position < state.BytesNeeded)
                            throw new EndOfStreamException();

                        state.BytesNeeded = 0;
                    }
                }

                return state.LastFrameResultObject;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read(ReadOnlySpan<byte> buffer, ref ReadState state, Encoding encoding)
        {
            var reader = new ByteReader(buffer, encoding);
            for (; ; )
            {
                switch (state.CurrentFrame.FrameType)
                {
                    case ReadFrameType.PropertyType: ReadPropertyType(ref reader, ref state); break;
                    case ReadFrameType.CoreType: ReadCoreType(ref reader, ref state); break;
                    case ReadFrameType.EnumType: ReadEnumType(ref reader, ref state); break;
                    case ReadFrameType.SpecialType: ReadSpecialType(ref reader, ref state); break;
                    case ReadFrameType.Object: ReadObject(ref reader, ref state); break;
                    case ReadFrameType.CoreTypeEnumerable: ReadCoreTypeEnumerable(ref reader, ref state); break;
                    case ReadFrameType.EnumTypeEnumerable: ReadEnumTypeEnumerable(ref reader, ref state); break;
                    //case ReadFrameType.SpecialTypeEnumerable: ReadSpecialTypeEnumerable(ref reader, ref state); break;
                    case ReadFrameType.ObjectEnumerable: ReadObjectEnumerable(ref reader, ref state); break;
                }
                if (state.Ended)
                {
                    return -1;
                }
                if (state.BytesNeeded > 0)
                {
                    return reader.Position;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadFrame ReadFrameFromType(ref ReadState state, SerializerTypeDetail typeDetail, bool hasReadPropertyType, bool nullFlags)
        {
            var frame = new ReadFrame();
            frame.TypeDetail = typeDetail;
            frame.NullFlags = nullFlags;

            if (state.IncludePropertyTypes && !hasReadPropertyType)
            {
                frame.FrameType = ReadFrameType.PropertyType;
                return frame;
            }
            else if (typeDetail == null)
            {
                throw new NotSupportedException("Cannot deserialize without type information");
            }

            if (typeDetail.TypeDetail.CoreType.HasValue)
            {
                frame.FrameType = ReadFrameType.CoreType;
                return frame;
            }

            if (typeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                frame.FrameType = ReadFrameType.EnumType;
                return frame;
            }

            if (typeDetail.TypeDetail.SpecialType.HasValue || typeDetail.TypeDetail.IsNullable && typeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            {
                frame.FrameType = ReadFrameType.SpecialType;
                return frame;
            }

            if (!typeDetail.TypeDetail.IsIEnumerableGeneric)
            {
                frame.FrameType = ReadFrameType.Object;
                return frame;
            }

            //Enumerable
            var innerTypeDetail = typeDetail.InnerTypeDetail;

            if (innerTypeDetail.TypeDetail.CoreType.HasValue)
            {
                frame.FrameType = ReadFrameType.CoreTypeEnumerable;
                return frame;
            }

            if (innerTypeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                frame.FrameType = ReadFrameType.EnumTypeEnumerable;
                return frame;
            }

            //if (innerTypeDetail.TypeDetail.SpecialType.HasValue || innerTypeDetail.TypeDetail.IsNullable && innerTypeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            //{
            //    frame.FrameType = ReadFrameType.SpecialTypeEnumerable;
            //    return frame;
            //}

            frame.FrameType = ReadFrameType.ObjectEnumerable;
            return frame;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadPropertyType(ref ByteReader reader, ref ReadState state)
        {
            if (state.CurrentFrame.HasReadPropertyType)
            {
                state.CurrentFrame.ResultObject = state.LastFrameResultObject;
                state.EndFrame();
                return;
            }

            var typeDetail = state.CurrentFrame.TypeDetail;

            if (!state.CurrentFrame.DrainBytes && typeDetail == null)
                throw new NotSupportedException("Cannot deserialize without type information");
            if (state.IncludePropertyTypes)
            {
                int sizeNeeded;
                if (!state.CurrentFrame.StringLength.HasValue)
                {
                    if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.StringLength = stringLength;
                }

                if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out var value, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }
                var typeName = value;

                var typeFromBytes = Discovery.GetTypeFromName(typeName);
                //overrides potentially boxed type with actual type if exists in assembly
                if (typeFromBytes != null)
                {
                    var newTypeDetail = GetTypeInformation(typeFromBytes, state.IndexSize, state.IgnoreIndexAttribute);

                    var typeDetailCheck = typeDetail.TypeDetail;
                    if (typeDetailCheck.IsNullable)
                        typeDetailCheck = typeDetailCheck.InnerTypeDetails[0];
                    var newTypeDetailCheck = newTypeDetail.TypeDetail;

                    if (newTypeDetailCheck.Type != typeDetailCheck.Type && !newTypeDetailCheck.Interfaces.Contains(typeDetailCheck.Type) && !newTypeDetail.TypeDetail.BaseTypes.Contains(typeDetailCheck.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.TypeDetail.Type.GetNiceName()}");

                    typeDetail = newTypeDetail;
                    state.CurrentFrame.TypeDetail = newTypeDetail;
                }
            }
            else if (typeDetail.Type.IsInterface && !typeDetail.TypeDetail.IsIEnumerableGeneric)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                typeDetail = GetTypeInformation(emptyImplementationType, state.IndexSize, state.IgnoreIndexAttribute);
            }

            state.CurrentFrame.HasReadPropertyType = true;

            var frame = ReadFrameFromType(ref state, typeDetail, true, state.CurrentFrame.NullFlags);
            state.PushFrame(frame);
        }

        private static void ReadCoreType(ref ByteReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var nullFlags = state.CurrentFrame.NullFlags;

            int sizeNeeded;
            switch (typeDetail.TypeDetail.CoreType)
            {
                case CoreType.Boolean:
                    {
                        if (!reader.TryReadBoolean(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Byte:
                    {
                        if (!reader.TryReadByte(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.SByte:
                    {
                        if (!reader.TryReadSByte(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Int16:
                    {
                        if (!reader.TryReadInt16(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.UInt16:
                    {
                        if (!reader.TryReadUInt16(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Int32:
                    {
                        if (!reader.TryReadInt32(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.UInt32:
                    {
                        if (!reader.TryReadUInt32(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Int64:
                    {
                        if (!reader.TryReadInt64(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.UInt64:
                    {
                        if (!reader.TryReadUInt64(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Single:
                    {
                        if (!reader.TryReadSingle(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Double:
                    {
                        if (!reader.TryReadDouble(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Decimal:
                    {
                        if (!reader.TryReadDecimal(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Char:
                    {
                        if (!reader.TryReadChar(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.DateTime:
                    {
                        if (!reader.TryReadDateTime(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.DateTimeOffset:
                    {
                        if (!reader.TryReadDateTimeOffset(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.TimeSpan:
                    {
                        if (!reader.TryReadTimeSpan(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Guid:
                    {
                        if (!reader.TryReadGuid(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }

                case CoreType.BooleanNullable:
                    {
                        if (!reader.TryReadBooleanNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.ByteNullable:
                    {
                        if (!reader.TryReadByteNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.SByteNullable:
                    {
                        if (!reader.TryReadSByteNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Int16Nullable:
                    {
                        if (!reader.TryReadInt16Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.UInt16Nullable:
                    {
                        if (!reader.TryReadUInt16Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Int32Nullable:
                    {
                        if (!reader.TryReadInt32Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.UInt32Nullable:
                    {
                        if (!reader.TryReadUInt32Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.Int64Nullable:
                    {
                        if (!reader.TryReadInt64Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.UInt64Nullable:
                    {
                        if (!reader.TryReadUInt64Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.SingleNullable:
                    {
                        if (!reader.TryReadSingleNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.DoubleNullable:
                    {
                        if (!reader.TryReadDoubleNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.DecimalNullable:
                    {
                        if (!reader.TryReadDecimalNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.CharNullable:
                    {
                        if (!reader.TryReadCharNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.DateTimeNullable:
                    {
                        if (!reader.TryReadDateTimeNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.DateTimeOffsetNullable:
                    {
                        if (!reader.TryReadDateTimeOffsetNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.TimeSpanNullable:
                    {
                        if (!reader.TryReadTimeSpanNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                case CoreType.GuidNullable:
                    {
                        if (!reader.TryReadGuidNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }

                case CoreType.String:
                    {
                        if (!state.CurrentFrame.StringLength.HasValue)
                        {
                            if (!reader.TryReadStringLength(nullFlags, out var stringLength, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.StringLength = stringLength;
                        }

                        if (state.CurrentFrame.StringLength.Value == 0)
                        {
                            state.CurrentFrame.ResultObject = String.Empty;
                            break;
                        }

                        if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.ResultObject = value;
                        break;
                    }
                default: throw new NotImplementedException();
            };

            state.EndFrame();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadEnumType(ref ByteReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var nullFlags = state.CurrentFrame.NullFlags;

            int sizeNeeded;
            object numValue;
            switch (typeDetail.TypeDetail.EnumUnderlyingType.Value)
            {
                case CoreType.Byte:
                    {
                        if (!reader.TryReadByte(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.SByte:
                    {
                        if (!reader.TryReadSByte(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.Int16:
                    {
                        if (!reader.TryReadInt16(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.UInt16:
                    {
                        if (!reader.TryReadUInt16(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.Int32:
                    {
                        if (!reader.TryReadInt32(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.UInt32:
                    {
                        if (!reader.TryReadUInt32(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.Int64:
                    {
                        if (!reader.TryReadInt64(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.UInt64:
                    {
                        if (!reader.TryReadUInt64(out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.ByteNullable:
                    {
                        if (!reader.TryReadByteNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.SByteNullable:
                    {
                        if (!reader.TryReadSByteNullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.Int16Nullable:
                    {
                        if (!reader.TryReadInt16Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.UInt16Nullable:
                    {
                        if (!reader.TryReadUInt16Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.Int32Nullable:
                    {
                        if (!reader.TryReadInt32Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.UInt32Nullable:
                    {
                        if (!reader.TryReadUInt32Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.Int64Nullable:
                    {
                        if (!reader.TryReadInt64Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                case CoreType.UInt64Nullable:
                    {
                        if (!reader.TryReadUInt64Nullable(nullFlags, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        numValue = value;
                        break;
                    }
                default: throw new NotImplementedException();
            };

            object enumValue;
            if (!typeDetail.TypeDetail.IsNullable)
                enumValue = Enum.ToObject(typeDetail.Type, numValue);
            else if (numValue != null)
                enumValue = Enum.ToObject(typeDetail.TypeDetail.InnerTypes[0], numValue);
            else
                enumValue = null;
            state.CurrentFrame.ResultObject = enumValue;

            state.EndFrame();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadSpecialType(ref ByteReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var nullFlags = state.CurrentFrame.NullFlags;

            var specialType = typeDetail.TypeDetail.IsNullable ? typeDetail.InnerTypeDetail.TypeDetail.SpecialType.Value : typeDetail.TypeDetail.SpecialType.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        int sizeNeeded;
                        if (!state.CurrentFrame.StringLength.HasValue)
                        {
                            if (!reader.TryReadStringLength(nullFlags, out var stringLength, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            if (!stringLength.HasValue)
                            {
                                state.CurrentFrame.ResultObject = null;
                                break;
                            }
                            state.CurrentFrame.StringLength = stringLength;
                        }

                        if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out var value, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        var typeName = value;
                        if (typeName == null)
                        {
                            //state.CurrentFrame.Obj = null;
                            state.EndFrame();
                            return;
                        }
                        state.CurrentFrame.ResultObject = Discovery.GetTypeFromName(typeName);
                        state.EndFrame();
                        return;
                    }
                case SpecialType.Dictionary:
                    {
                        if (!state.CurrentFrame.HasObjectStarted)
                        {
                            if (nullFlags)
                            {
                                if (!reader.TryReadIsNull(out var value, out var moreBytesNeeded))
                                {
                                    state.BytesNeeded = moreBytesNeeded;
                                    return;
                                }
                                if (value)
                                {
                                    //state.CurrentFrame.Obj = null;
                                    state.EndFrame();
                                    return;
                                }
                            }
                            state.CurrentFrame.HasObjectStarted = true;

                            state.PushFrame(new ReadFrame()
                            {
                                FrameType = ReadFrameType.ObjectEnumerable,
                                TypeDetail = typeDetail,
                                NullFlags = false
                            });

                            return;
                        }

                        var innerValue = state.LastFrameResultObject;
                        var innerItemEnumerable = TypeAnalyzer.GetGenericType(enumerableType, typeDetail.TypeDetail.IEnumerableGenericInnerType);

                        if (typeDetail.Type.IsInterface)
                        {
                            var dictionaryGenericType = TypeAnalyzer.GetGenericType(dictionaryType, (Type[])typeDetail.TypeDetail.IEnumerableGenericInnerTypeDetails.InnerTypes);
                            state.CurrentFrame.ResultObject = Instantiator.Create(dictionaryGenericType, new Type[] { innerItemEnumerable }, innerValue);
                            state.EndFrame();
                            return;
                        }
                        else
                        {
                            state.CurrentFrame.ResultObject = Instantiator.Create(typeDetail.Type, new Type[] { innerItemEnumerable }, innerValue);
                            state.EndFrame();
                            return;
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadObject(ref ByteReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var nullFlags = state.CurrentFrame.NullFlags;

            if (nullFlags && !state.CurrentFrame.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out var sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }

                if (isNull)
                {
                    //state.CurrentFrame.Obj = null;
                    state.EndFrame();
                    return;
                }

                state.CurrentFrame.HasNullChecked = true;
            }

            if (!state.CurrentFrame.HasObjectStarted)
            {
                if (!state.CurrentFrame.DrainBytes)
                    state.CurrentFrame.ResultObject = typeDetail.TypeDetail.Creator();
                state.CurrentFrame.HasObjectStarted = true;
            }

            for (; ; )
            {
                if (!state.CurrentFrame.DrainBytes && state.CurrentFrame.ObjectProperty != null)
                {
                    state.CurrentFrame.ObjectProperty.Setter(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                    state.CurrentFrame.ObjectProperty = null;
                }

                SerializerMemberDetail property = null;

                if (state.UsePropertyNames)
                {
                    int sizeNeeded;
                    if (!state.CurrentFrame.StringLength.HasValue)
                    {
                        if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.StringLength = stringLength;
                    }

                    if (state.CurrentFrame.StringLength.Value == 0)
                    {
                        state.EndFrame();
                        return;
                    }

                    if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out var name, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.StringLength = null;
                    property = typeDetail.IndexedProperties.Values.FirstOrDefault(x => x.Name == name);
                    state.CurrentFrame.ObjectProperty = property;

                    if (property == null)
                    {
                        if (!state.UsePropertyNames && !state.IncludePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {name} undefined and no types.");

                        //consume bytes but object does not have property
                        var frame = ReadFrameFromType(ref state, null, false, false);
                        state.PushFrame(frame);
                        state.CurrentFrame.DrainBytes = true;
                        return;
                    }
                    else
                    {
                        var frame = ReadFrameFromType(ref state, property.SerailzierTypeDetails, false, false);
                        state.PushFrame(frame);
                        return;
                    }
                }
                else
                {
                    ushort propertyIndex;
                    switch (state.IndexSize)
                    {
                        case ByteSerializerIndexSize.Byte:
                            {
                                if (!reader.TryReadByte(out var value, out var sizeNeeded))
                                {
                                    state.BytesNeeded = sizeNeeded;
                                    return;
                                }
                                propertyIndex = value;
                                break;
                            }
                        case ByteSerializerIndexSize.UInt16:
                            {
                                if (!reader.TryReadUInt16(out var value, out var sizeNeeded))
                                {
                                    state.BytesNeeded = sizeNeeded;
                                    return;
                                }
                                propertyIndex = value;
                                break;
                            }
                        default: throw new NotImplementedException();
                    }

                    if (propertyIndex == endObjectFlagUShort)
                    {
                        state.EndFrame();
                        return;
                    }

                    if (typeDetail.IndexedProperties.Keys.Contains(propertyIndex))
                        property = typeDetail.IndexedProperties[propertyIndex];
                    state.CurrentFrame.ObjectProperty = property;

                    if (property == null)
                    {
                        if (!state.UsePropertyNames && !state.IncludePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {propertyIndex} undefined and no types.");

                        //consume bytes but object does not have property
                        var frame = ReadFrameFromType(ref state, null, false, false);
                        state.PushFrame(frame);
                        state.CurrentFrame.DrainBytes = true;
                        return;
                    }
                    else
                    {
                        var frame = ReadFrameFromType(ref state, property.SerailzierTypeDetails, false, false);
                        state.PushFrame(frame);
                        return;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadCoreTypeEnumerable(ref ByteReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var asList = !typeDetail.TypeDetail.Type.IsArray && typeDetail.TypeDetail.IsIList;
            typeDetail = typeDetail.InnerTypeDetail;

            int sizeNeeded;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out var value, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.EnumerableLength = value;
            }

            var length = state.CurrentFrame.EnumerableLength.Value;

            if (asList)
            {
                switch (typeDetail.TypeDetail.CoreType)
                {
                    case CoreType.Boolean:
                        {
                            if (!reader.TryReadBooleanList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Byte:
                        {
                            if (!reader.TryReadByteList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.SByte:
                        {
                            if (!reader.TryReadSByteList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int16:
                        {
                            if (!reader.TryReadInt16List(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt16:
                        {
                            if (!reader.TryReadUInt16List(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int32:
                        {
                            if (!reader.TryReadInt32List(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt32:
                        {
                            if (!reader.TryReadUInt32List(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int64:
                        {
                            if (!reader.TryReadInt64List(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt64:
                        {
                            if (!reader.TryReadUInt64List(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Single:
                        {
                            if (!reader.TryReadSingleList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Double:
                        {
                            if (!reader.TryReadDoubleList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Decimal:
                        {
                            if (!reader.TryReadDecimalList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Char:
                        {
                            if (!reader.TryReadCharList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DateTime:
                        {
                            if (!reader.TryReadDateTimeList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DateTimeOffset:
                        {
                            if (!reader.TryReadDateTimeOffsetList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.TimeSpan:
                        {
                            if (!reader.TryReadTimeSpanList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Guid:
                        {
                            if (!reader.TryReadGuidList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }

                    case CoreType.BooleanNullable:
                        {
                            if (!reader.TryReadBooleanNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.ByteNullable:
                        {
                            if (!reader.TryReadByteNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.SByteNullable:
                        {
                            if (!reader.TryReadSByteNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int16Nullable:
                        {
                            if (!reader.TryReadInt16NullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt16Nullable:
                        {
                            if (!reader.TryReadUInt16NullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int32Nullable:
                        {
                            if (!reader.TryReadInt32NullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt32Nullable:
                        {
                            if (!reader.TryReadUInt32NullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int64Nullable:
                        {
                            if (!reader.TryReadInt64NullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt64Nullable:
                        {
                            if (!reader.TryReadUInt64NullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.SingleNullable:
                        {
                            if (!reader.TryReadSingleNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DoubleNullable:
                        {
                            if (!reader.TryReadDoubleNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DecimalNullable:
                        {
                            if (!reader.TryReadDecimalNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.CharNullable:
                        {
                            if (!reader.TryReadCharNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DateTimeNullable:
                        {
                            if (!reader.TryReadDateTimeNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DateTimeOffsetNullable:
                        {
                            if (!reader.TryReadDateTimeOffsetNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.TimeSpanNullable:
                        {
                            if (!reader.TryReadTimeSpanNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.GuidNullable:
                        {
                            if (!reader.TryReadGuidNullableList(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }

                    case CoreType.String:
                        {
                            if (!state.CurrentFrame.HasObjectStarted)
                            {
                                state.CurrentFrame.EnumerableList = (IList)typeDetail.ListCreator(length);
                                state.CurrentFrame.ResultObject = state.CurrentFrame.EnumerableList;
                                state.CurrentFrame.HasObjectStarted = true;
                            }

                            if (length == 0)
                                break;

                            for (; ; )
                            {
                                if (!state.CurrentFrame.StringLength.HasValue)
                                {
                                    if (!reader.TryReadStringLength(true, out var stringLength, out sizeNeeded))
                                    {
                                        state.BytesNeeded = sizeNeeded;
                                        return;
                                    }
                                    if (!stringLength.HasValue)
                                    {
                                        _ = state.CurrentFrame.EnumerableList.Add(null);
                                        state.CurrentFrame.EnumerablePosition++;
                                        if (state.CurrentFrame.EnumerablePosition == length)
                                            break;
                                        continue;
                                    }
                                    state.CurrentFrame.StringLength = stringLength;
                                }

                                string str;
                                if (state.CurrentFrame.StringLength.Value == 0)
                                {
                                    str = String.Empty;
                                }
                                else if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out str, out sizeNeeded))
                                {
                                    state.BytesNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.StringLength = null;

                                _ = state.CurrentFrame.EnumerableList.Add(str);
                                state.CurrentFrame.EnumerablePosition++;
                                if (state.CurrentFrame.EnumerablePosition == length)
                                    break;
                            }
                            break;
                        }
                    default: throw new NotImplementedException();
                };
            }
            else
            {
                switch (typeDetail.TypeDetail.CoreType)
                {
                    case CoreType.Boolean:
                        {
                            if (!reader.TryReadBooleanArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Byte:
                        {
                            if (!reader.TryReadByteArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.SByte:
                        {
                            if (!reader.TryReadSByteArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int16:
                        {
                            if (!reader.TryReadInt16Array(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt16:
                        {
                            if (!reader.TryReadUInt16Array(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int32:
                        {
                            if (!reader.TryReadInt32Array(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt32:
                        {
                            if (!reader.TryReadUInt32Array(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int64:
                        {
                            if (!reader.TryReadInt64Array(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt64:
                        {
                            if (!reader.TryReadUInt64Array(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Single:
                        {
                            if (!reader.TryReadSingleArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Double:
                        {
                            if (!reader.TryReadDoubleArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Decimal:
                        {
                            if (!reader.TryReadDecimalArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Char:
                        {
                            if (!reader.TryReadCharArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DateTime:
                        {
                            if (!reader.TryReadDateTimeArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DateTimeOffset:
                        {
                            if (!reader.TryReadDateTimeOffsetArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.TimeSpan:
                        {
                            if (!reader.TryReadTimeSpanArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Guid:
                        {
                            if (!reader.TryReadGuidArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }

                    case CoreType.BooleanNullable:
                        {
                            if (!reader.TryReadBooleanNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.ByteNullable:
                        {
                            if (!reader.TryReadByteNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.SByteNullable:
                        {
                            if (!reader.TryReadSByteNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int16Nullable:
                        {
                            if (!reader.TryReadInt16NullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt16Nullable:
                        {
                            if (!reader.TryReadUInt16NullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int32Nullable:
                        {
                            if (!reader.TryReadInt32NullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt32Nullable:
                        {
                            if (!reader.TryReadUInt32NullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.Int64Nullable:
                        {
                            if (!reader.TryReadInt64NullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.UInt64Nullable:
                        {
                            if (!reader.TryReadUInt64NullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.SingleNullable:
                        {
                            if (!reader.TryReadSingleNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DoubleNullable:
                        {
                            if (!reader.TryReadDoubleNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DecimalNullable:
                        {
                            if (!reader.TryReadDecimalNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.CharNullable:
                        {
                            if (!reader.TryReadCharNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DateTimeNullable:
                        {
                            if (!reader.TryReadDateTimeNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.DateTimeOffsetNullable:
                        {
                            if (!reader.TryReadDateTimeOffsetNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.TimeSpanNullable:
                        {
                            if (!reader.TryReadTimeSpanNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }
                    case CoreType.GuidNullable:
                        {
                            if (!reader.TryReadGuidNullableArray(length, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.ResultObject = value;
                            break;
                        }

                    case CoreType.String:
                        {
                            if (!state.CurrentFrame.HasObjectStarted)
                            {
                                state.CurrentFrame.EnumerableArray = Array.CreateInstance(typeDetail.Type, length);
                                state.CurrentFrame.ResultObject = state.CurrentFrame.EnumerableArray;
                                state.CurrentFrame.HasObjectStarted = true;
                            }

                            if (length == 0)
                                break;

                            for (; ; )
                            {
                                if (!state.CurrentFrame.StringLength.HasValue)
                                {
                                    if (!reader.TryReadStringLength(true, out var stringLength, out sizeNeeded))
                                    {
                                        state.BytesNeeded = sizeNeeded;
                                        return;
                                    }
                                    if (!stringLength.HasValue)
                                    {
                                        state.CurrentFrame.EnumerablePosition++;
                                        if (state.CurrentFrame.EnumerablePosition == length)
                                            break;
                                        continue;
                                    }
                                    state.CurrentFrame.StringLength = stringLength;
                                }

                                string str;
                                if (state.CurrentFrame.StringLength.Value == 0)
                                {
                                    str = String.Empty;
                                }
                                else if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out str, out sizeNeeded))
                                {
                                    state.BytesNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.StringLength = null;

                                state.CurrentFrame.EnumerableArray.SetValue(str, state.CurrentFrame.EnumerablePosition);
                                state.CurrentFrame.EnumerablePosition++;
                                if (state.CurrentFrame.EnumerablePosition == length)
                                    break;
                            }
                            break;
                        }
                    default: throw new NotImplementedException();
                };
            }
            state.EndFrame();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadEnumTypeEnumerable(ref ByteReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var nullFlags = true;
            var asList = !typeDetail.TypeDetail.Type.IsArray && typeDetail.TypeDetail.IsIList;
            typeDetail = typeDetail.InnerTypeDetail;

            int length;
            int sizeNeeded;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.EnumerableLength = length;

                if (!state.CurrentFrame.DrainBytes)
                {
                    if (!asList)
                    {
                        state.CurrentFrame.EnumerableArray = Array.CreateInstance(typeDetail.Type, length);
                        state.CurrentFrame.ResultObject = state.CurrentFrame.EnumerableArray;
                    }
                    else
                    {
                        state.CurrentFrame.EnumerableList = (IList)typeDetail.ListCreator(length);
                        state.CurrentFrame.ResultObject = state.CurrentFrame.EnumerableList;
                    }
                }

                if (length == 0)
                {
                    state.EndFrame();
                    return;
                }
            }

            length = state.CurrentFrame.EnumerableLength.Value;

            for (; ; )
            {
                object numValue;
                switch (typeDetail.TypeDetail.EnumUnderlyingType.Value)
                {
                    case CoreType.Byte:
                        {
                            if (!reader.TryReadByte(out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.SByte:
                        {
                            if (!reader.TryReadSByte(out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.Int16:
                        {
                            if (!reader.TryReadInt16(out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.UInt16:
                        {
                            if (!reader.TryReadUInt16(out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.Int32:
                        {
                            if (!reader.TryReadInt32(out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.UInt32:
                        {
                            if (!reader.TryReadUInt32(out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.Int64:
                        {
                            if (!reader.TryReadInt64(out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.UInt64:
                        {
                            if (!reader.TryReadUInt64(out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.ByteNullable:
                        {
                            if (!reader.TryReadByteNullable(nullFlags, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.SByteNullable:
                        {
                            if (!reader.TryReadSByteNullable(nullFlags, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.Int16Nullable:
                        {
                            if (!reader.TryReadInt16Nullable(nullFlags, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.UInt16Nullable:
                        {
                            if (!reader.TryReadUInt16Nullable(nullFlags, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.Int32Nullable:
                        {
                            if (!reader.TryReadInt32Nullable(nullFlags, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.UInt32Nullable:
                        {
                            if (!reader.TryReadUInt32Nullable(nullFlags, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.Int64Nullable:
                        {
                            if (!reader.TryReadInt64Nullable(nullFlags, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    case CoreType.UInt64Nullable:
                        {
                            if (!reader.TryReadUInt64Nullable(nullFlags, out var value, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            numValue = value;
                            break;
                        }
                    default: throw new NotImplementedException();
                };

                object enumValue;
                if (!typeDetail.TypeDetail.IsNullable)
                    enumValue = Enum.ToObject(typeDetail.Type, numValue);
                else if (numValue != null)
                    enumValue = Enum.ToObject(typeDetail.TypeDetail.InnerTypes[0], numValue);
                else
                    enumValue = null;

                if (!state.CurrentFrame.DrainBytes)
                {
                    if (asList)
                        _ = state.CurrentFrame.EnumerableList.Add(enumValue);
                    else
                        state.CurrentFrame.EnumerableArray.SetValue(enumValue, state.CurrentFrame.EnumerablePosition);
                }
                state.CurrentFrame.EnumerablePosition++;

                if (state.CurrentFrame.EnumerablePosition == length)
                {
                    state.EndFrame();
                    return;
                }
            }
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static void ReadSpecialTypeEnumerable(ref ByteReader reader, ref ReadState state)
        //{
        //    throw new NotImplementedException();
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadObjectEnumerable(ref ByteReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            var asList = !typeDetail.TypeDetail.Type.IsArray && typeDetail.TypeDetail.IsIList;
            typeDetail = typeDetail.InnerTypeDetail;

            int length;
            int sizeNeeded;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.EnumerableLength = length;

                if (!state.CurrentFrame.DrainBytes)
                {
                    if (!asList)
                    {
                        state.CurrentFrame.EnumerableArray = Array.CreateInstance(typeDetail.Type, length);
                        state.CurrentFrame.ResultObject = state.CurrentFrame.EnumerableArray;
                    }
                    else
                    {
                        state.CurrentFrame.EnumerableList = (IList)typeDetail.ListCreator(length);
                        state.CurrentFrame.ResultObject = state.CurrentFrame.EnumerableList;
                    }
                }

                if (length == 0)
                {
                    state.EndFrame();
                    return;
                }
            }

            length = state.CurrentFrame.EnumerableLength.Value;

            for (; ; )
            {
                if (!state.CurrentFrame.HasNullChecked)
                {
                    if (!reader.TryReadIsNull(out var isNull, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }

                    if (isNull)
                    {
                        if (asList)
                            _ = state.CurrentFrame.EnumerableList.Add(null);
                        state.CurrentFrame.EnumerablePosition++;
                        if (state.CurrentFrame.EnumerablePosition == length)
                        {
                            state.EndFrame();
                            return;
                        }
                        continue;
                    }
                    state.CurrentFrame.HasNullChecked = true;
                }

                if (!state.CurrentFrame.HasObjectStarted)
                {
                    state.CurrentFrame.HasObjectStarted = true;
                    var frame = ReadFrameFromType(ref state, typeDetail, false, false);
                    state.PushFrame(frame);
                    return;
                }

                if (!state.CurrentFrame.DrainBytes)
                {
                    if (asList)
                        _ = state.CurrentFrame.EnumerableList.Add(state.LastFrameResultObject);
                    else
                        state.CurrentFrame.EnumerableArray.SetValue(state.LastFrameResultObject, state.CurrentFrame.EnumerablePosition);
                }
                state.CurrentFrame.HasObjectStarted = false;
                state.CurrentFrame.HasNullChecked = false;
                state.CurrentFrame.EnumerablePosition++;

                if (state.CurrentFrame.EnumerablePosition == length)
                {
                    state.EndFrame();
                    return;
                }
            }
        }
    }
}