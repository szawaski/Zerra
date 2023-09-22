// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class ByteSerializer
    {
        public static byte[] NewSerializeStackBased(object obj, ByteSerializerOptions options = null)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return SerializeStackBased(obj, obj.GetType(), options);
        }
        public static byte[] SerializeStackBased<T>(T obj, ByteSerializerOptions options = null)
        {
            return SerializeStackBased(obj, typeof(T), options);
        }
        public static byte[] SerializeStackBased(object obj, Type type, ByteSerializerOptions options = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            options ??= defaultOptions;

            var typeDetail = GetTypeInformation(type, options.IndexSize, options.IgnoreIndexAttribute);
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif
            var position = 0;

            try
            {
                var state = new WriteState()
                {
                    UsePropertyNames = options.UsePropertyNames,
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSize = options.IndexSize
                };
                state.CurrentFrame = WriteFrameFromType(ref state, obj, typeDetail, false, true);

                for (; ; )
                {
                    var usedBytes = Write(buffer.AsSpan().Slice(position), ref state, options.Encoding);

                    position += usedBytes;

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length - position)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded + position);

                        state.BytesNeeded = 0;
                    }
                }
            }
            finally
            {
                BufferArrayPool<byte>.Return(buffer);
            }

            var result = new byte[position];
            Buffer.BlockCopy(buffer, 0, result, 0, position);
            return result;
        }

        public static void Serialize(Stream stream, object obj, ByteSerializerOptions options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Serialize(stream, obj, obj.GetType());
        }
        public static void Serialize<T>(Stream stream, T obj, ByteSerializerOptions options = null)
        {
            Serialize(stream, obj, typeof(T), options);
        }
        public static void Serialize(Stream stream, object obj, Type type, ByteSerializerOptions options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            options ??= defaultOptions;

            var typeDetail = GetTypeInformation(type, options.IndexSize, options.IgnoreIndexAttribute);
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var state = new WriteState()
                {
                    UsePropertyNames = options.UsePropertyNames,
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSize = options.IndexSize
                };
                state.CurrentFrame = WriteFrameFromType(ref state, obj, typeDetail, false, true);

                for (; ; )
                {
                    var usedBytes = Write(buffer, ref state, options.Encoding);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        public static Task SerializeAsync(Stream stream, object obj, ByteSerializerOptions options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return Task.CompletedTask;

            var type = obj.GetType();

            return SerializeAsync(stream, obj, type, options);
        }
        public static Task SerializeAsync<T>(Stream stream, T obj, ByteSerializerOptions options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return Task.CompletedTask;

            var type = typeof(T);

            return SerializeAsync(stream, obj, type, options);
        }
        public static async Task SerializeAsync(Stream stream, object obj, Type type, ByteSerializerOptions options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = GetTypeInformation(type, options.IndexSize, options.IgnoreIndexAttribute);
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var state = new WriteState()
                {
                    UsePropertyNames = options.UsePropertyNames,
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSize = options.IndexSize
                };
                state.CurrentFrame = WriteFrameFromType(ref state, obj, typeDetail, false, true);

                for (; ; )
                {
                    var usedBytes = Write(buffer, ref state, options.Encoding);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Write(Span<byte> buffer, ref WriteState state, Encoding encoding)
        {
            var writer = new ByteWriter(buffer, encoding);
            for (; ; )
            {
                switch (state.CurrentFrame.FrameType)
                {
                    case WriteFrameType.PropertyType: WritePropertyType(ref writer, ref state); break;
                    case WriteFrameType.CoreType: WriteCoreType(ref writer, ref state); break;
                    case WriteFrameType.EnumType: WriteEnumType(ref writer, ref state); break;
                    case WriteFrameType.SpecialType: WriteSpecialType(ref writer, ref state); break;
                    case WriteFrameType.Object: WriteObject(ref writer, ref state); break;
                    case WriteFrameType.CoreTypeEnumerable: WriteCoreTypeEnumerable(ref writer, ref state); break;
                    case WriteFrameType.EnumTypeEnumerable: WriteEnumTypeEnumerable(ref writer, ref state); break;
                    //case WriteFrameType.SpecialTypeEnumerable: WriteSpecialTypeEnumerable(ref reader, ref state); break;
                    case WriteFrameType.ObjectEnumerable: WriteObjectEnumerable(ref writer, ref state); break;
                }
                if (state.Ended)
                {
                    return writer.Length;
                }
                if (state.BytesNeeded > 0)
                {
                    return writer.Length;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static WriteFrame WriteFrameFromType(ref WriteState state, object obj, SerializerTypeDetail typeDetail, bool hasWrittenPropertyType, bool nullFlags)
        {
            var frame = new WriteFrame();
            frame.TypeDetail = typeDetail;
            frame.NullFlags = nullFlags;
            frame.Object = obj;

            if (state.IncludePropertyTypes && !hasWrittenPropertyType)
            {
                frame.FrameType = WriteFrameType.PropertyType;
                return frame;
            }

            if (typeDetail.TypeDetail.CoreType.HasValue)
            {
                frame.FrameType = WriteFrameType.CoreType;
                return frame;
            }

            if (typeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                frame.FrameType = WriteFrameType.EnumType;
                return frame;
            }

            if (typeDetail.TypeDetail.SpecialType.HasValue || typeDetail.TypeDetail.IsNullable && typeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            {
                frame.FrameType = WriteFrameType.SpecialType;
                return frame;
            }

            if (!typeDetail.TypeDetail.IsIEnumerableGeneric)
            {
                frame.FrameType = WriteFrameType.Object;
                return frame;
            }

            //Enumerable
            var innerTypeDetail = typeDetail.InnerTypeDetail;

            if (innerTypeDetail.TypeDetail.CoreType.HasValue)
            {
                frame.FrameType = WriteFrameType.CoreTypeEnumerable;
                return frame;
            }

            if (innerTypeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                frame.FrameType = WriteFrameType.EnumTypeEnumerable;
                return frame;
            }

            //if (innerTypeDetail.TypeDetail.SpecialType.HasValue || innerTypeDetail.TypeDetail.IsNullable && innerTypeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            //{
            //    frame.FrameType = ReadFrameType.SpecialTypeEnumerable;
            //    return frame;
            //}

            frame.FrameType = WriteFrameType.ObjectEnumerable;
            return frame;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WritePropertyType(ref ByteWriter writer, ref WriteState state)
        {
            if (state.CurrentFrame.HasWrittenPropertyType)
            {
                state.EndFrame();
                return;
            }

            var typeDetail = state.CurrentFrame.TypeDetail;

            if (state.IncludePropertyTypes)
            {
                var typeFromValue = state.CurrentFrame.Object.GetType();
                var typeName = typeFromValue.FullName;

                int sizeNeeded;
                if (!writer.TryWrite(typeName, false, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }

                typeDetail = GetTypeInformation(typeFromValue, state.IndexSize, state.IgnoreIndexAttribute);
            }
            else if (typeDetail.Type.IsInterface && !typeDetail.TypeDetail.IsIEnumerableGeneric)
            {
                var objectType = state.CurrentFrame.Object.GetType();
                typeDetail = GetTypeInformation(objectType, state.IndexSize, state.IgnoreIndexAttribute);
            }

            state.CurrentFrame.HasWrittenPropertyType = true;

            var frame = WriteFrameFromType(ref state, state.CurrentFrame.Object, typeDetail, true, state.CurrentFrame.NullFlags);
            state.PushFrame(frame);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteCoreType(ref ByteWriter writer, ref WriteState state)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            int sizeNeeded;
            switch (state.CurrentFrame.TypeDetail.TypeDetail.CoreType)
            {
                case CoreType.Boolean:
                    if (!writer.TryWrite((bool)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Byte:
                    if (!writer.TryWrite((byte)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SByte:
                    if (!writer.TryWrite((sbyte)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int16:
                    if (!writer.TryWrite((short)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt16:
                    if (!writer.TryWrite((ushort)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int32:
                    if (!writer.TryWrite((int)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt32:
                    if (!writer.TryWrite((uint)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int64:
                    if (!writer.TryWrite((long)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt64:
                    if (!writer.TryWrite((ulong)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Single:
                    if (!writer.TryWrite((float)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Double:
                    if (!writer.TryWrite((double)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Decimal:
                    if (!writer.TryWrite((decimal)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Char:
                    if (!writer.TryWrite((char)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DateTime:
                    if (!writer.TryWrite((DateTime)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DateTimeOffset:
                    if (!writer.TryWrite((DateTimeOffset)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.TimeSpan:
                    if (!writer.TryWrite((TimeSpan)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Guid:
                    if (!writer.TryWrite((Guid)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;

                case CoreType.BooleanNullable:
                    if (!writer.TryWrite((bool?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.ByteNullable:
                    if (!writer.TryWrite((byte?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SByteNullable:
                    if (!writer.TryWrite((sbyte?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int16Nullable:
                    if (!writer.TryWrite((short?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt16Nullable:
                    if (!writer.TryWrite((ushort?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int32Nullable:
                    if (!writer.TryWrite((int?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt32Nullable:
                    if (!writer.TryWrite((uint?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int64Nullable:
                    if (!writer.TryWrite((long?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt64Nullable:
                    if (!writer.TryWrite((ulong?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SingleNullable:
                    if (!writer.TryWrite((float?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DoubleNullable:
                    if (!writer.TryWrite((double?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DecimalNullable:
                    if (!writer.TryWrite((decimal?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.CharNullable:
                    if (!writer.TryWrite((char?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DateTimeNullable:
                    if (!writer.TryWrite((DateTime?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DateTimeOffsetNullable:
                    if (!writer.TryWrite((DateTimeOffset?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.TimeSpanNullable:
                    if (!writer.TryWrite((TimeSpan?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.GuidNullable:
                    if (!writer.TryWrite((Guid?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;

                case CoreType.String:
                    if (!writer.TryWrite((string)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            state.EndFrame();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteEnumType(ref ByteWriter writer, ref WriteState state)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            int sizeNeeded;
            switch (state.CurrentFrame.TypeDetail.TypeDetail.EnumUnderlyingType)
            {
                case CoreType.Byte:
                    if (!writer.TryWrite((byte)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SByte:
                    if (!writer.TryWrite((sbyte)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int16:
                    if (!writer.TryWrite((short)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt16:
                    if (!writer.TryWrite((ushort)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int32:
                    if (!writer.TryWrite((int)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt32:
                    if (!writer.TryWrite((uint)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int64:
                    if (!writer.TryWrite((long)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt64:
                    if (!writer.TryWrite((ulong)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;

                case CoreType.ByteNullable:
                    if (!writer.TryWrite(state.CurrentFrame.Object == null ? null : (byte?)(byte)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SByteNullable:
                    if (!writer.TryWrite(state.CurrentFrame.Object == null ? null : (sbyte?)(sbyte)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int16Nullable:
                    if (!writer.TryWrite(state.CurrentFrame.Object == null ? null : (short?)(short)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt16Nullable:
                    if (!writer.TryWrite(state.CurrentFrame.Object == null ? null : (ushort?)(ushort)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int32Nullable:
                    if (!writer.TryWrite(state.CurrentFrame.Object == null ? null : (int?)(int)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt32Nullable:
                    if (!writer.TryWrite(state.CurrentFrame.Object == null ? null : (uint?)(uint)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int64Nullable:
                    if (!writer.TryWrite(state.CurrentFrame.Object == null ? null : (long?)(long)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt64Nullable:
                    if (!writer.TryWrite(state.CurrentFrame.Object == null ? null : (ulong?)(ulong)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            state.EndFrame();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteSpecialType(ref ByteWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var specialType = typeDetail.TypeDetail.IsNullable ? typeDetail.InnerTypeDetail.TypeDetail.SpecialType.Value : typeDetail.TypeDetail.SpecialType.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        var valueType = state.CurrentFrame.Object == null ? null : (Type)state.CurrentFrame.Object;
                        if (!writer.TryWrite(valueType?.FullName, state.CurrentFrame.NullFlags, out var sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        };
                        state.EndFrame();
                    }
                    break;
                case SpecialType.Dictionary:
                    {
                        if (!state.CurrentFrame.ObjectInProgress)
                        {
                            if (state.CurrentFrame.Object != null)
                            {
                                if (state.CurrentFrame.NullFlags)
                                {
                                    if (!state.CurrentFrame.HasWrittenIsNull)
                                    {
                                        if (!writer.TryWriteNotNull(out var sizeNeeded))
                                        {
                                            state.BytesNeeded = sizeNeeded;
                                            return;
                                        };
                                        state.CurrentFrame.HasWrittenIsNull = true;
                                    }
                                }
                                var method = TypeAnalyzer.GetGenericMethodDetail(enumerableToArrayMethod, typeDetail.TypeDetail.IEnumerableGenericInnerType);
                                var innerValue = (ICollection)method.Caller(null, new object[] { state.CurrentFrame.Object });

                                state.CurrentFrame.ObjectInProgress = true;
                                state.PushFrame(new WriteFrame()
                                {
                                    FrameType = WriteFrameType.ObjectEnumerable,
                                    TypeDetail = typeDetail,
                                    NullFlags = false,
                                    Object = innerValue
                                });
                                return;
                            }
                            else if (state.CurrentFrame.NullFlags)
                            {
                                if (!writer.TryWriteNull(out var sizeNeeded))
                                {
                                    state.BytesNeeded = sizeNeeded;
                                    return;
                                };
                            }
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            state.EndFrame();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteObject(ref ByteWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var nullFlags = state.CurrentFrame.NullFlags;

            var value = state.CurrentFrame.Object;

            int sizeNeeded;
            if (!state.CurrentFrame.HasWrittenIsNull)
            {
                if (nullFlags)
                {
                    if (value == null)
                    {
                        if (!writer.TryWriteNull(out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        state.EndFrame();
                        return;
                    }
                    if (!writer.TryWriteNotNull(out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                }
                state.CurrentFrame.HasWrittenIsNull = true;
            }

            state.CurrentFrame.MemberEnumerator ??= typeDetail.IndexedProperties.GetEnumerator();

            while (state.CurrentFrame.EnumeratorObjectInProgress || state.CurrentFrame.MemberEnumerator.MoveNext())
            {
                var indexProperty = state.CurrentFrame.MemberEnumerator.Current;
                state.CurrentFrame.EnumeratorObjectInProgress = true;

                var propertyValue = indexProperty.Value.Getter(value);
                if (propertyValue == null)
                {
                    state.CurrentFrame.EnumeratorObjectInProgress = false;
                    continue;
                }

                if (state.UsePropertyNames)
                {
                    if (!writer.TryWrite(indexProperty.Value.Name, false, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                }
                else
                {
                    switch (state.IndexSize)
                    {
                        case ByteSerializerIndexSize.Byte:
                            if (!writer.TryWrite((byte)indexProperty.Key, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            break;
                        case ByteSerializerIndexSize.UInt16:
                            if (!writer.TryWrite(indexProperty.Key, out sizeNeeded))
                            {
                                state.BytesNeeded = sizeNeeded;
                                return;
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                state.CurrentFrame.EnumeratorObjectInProgress = false;
                var frame = WriteFrameFromType(ref state, propertyValue, indexProperty.Value.SerailzierTypeDetails, false, false);
                state.PushFrame(frame);
                return;
            }

            if (state.UsePropertyNames)
            {
                if (!writer.TryWrite(0, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }
            }
            else
            {
                switch (state.IndexSize)
                {
                    case ByteSerializerIndexSize.Byte:
                        if (!writer.TryWrite(endObjectFlagByte, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        break;
                    case ByteSerializerIndexSize.UInt16:
                        if (!writer.TryWrite(endObjectFlagUInt16, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return;
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            state.EndFrame();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteCoreTypeEnumerable(ref ByteWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            typeDetail = typeDetail.InnerTypeDetail;

            var values = state.CurrentFrame.Object;

            int sizeNeeded;

            int length;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (typeDetail.TypeDetail.IsICollection)
                {
                    var collection = (ICollection)values;
                    length = collection.Count;
                }
                else if (typeDetail.TypeDetail.IsICollectionGeneric)
                {
                    length = (int)typeDetail.TypeDetail.GetMember("Count").Getter(values);
                }
                else
                {
                    var enumerable = (IEnumerable)values;
                    length = 0;
                    foreach (var item in enumerable)
                        length++;
                }

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.EnumerableLength = length;
            }
            else
            {
                length = state.CurrentFrame.EnumerableLength.Value;
            }


            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (typeDetail.TypeDetail.CoreType)
            {
                case CoreType.Boolean:
                    if (!writer.TryWrite((IEnumerable<bool>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Byte:
                    if (!writer.TryWrite((IEnumerable<byte>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SByte:
                    if (!writer.TryWrite((IEnumerable<sbyte>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int16:
                    if (!writer.TryWrite((IEnumerable<short>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt16:
                    if (!writer.TryWrite((IEnumerable<ushort>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int32:
                    if (!writer.TryWrite((IEnumerable<int>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt32:
                    if (!writer.TryWrite((IEnumerable<uint>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int64:
                    if (!writer.TryWrite((IEnumerable<long>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt64:
                    if (!writer.TryWrite((IEnumerable<ulong>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Single:
                    if (!writer.TryWrite((IEnumerable<float>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Double:
                    if (!writer.TryWrite((IEnumerable<double>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Decimal:
                    if (!writer.TryWrite((IEnumerable<decimal>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Char:
                    if (!writer.TryWrite((IEnumerable<char>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DateTime:
                    if (!writer.TryWrite((IEnumerable<DateTime>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DateTimeOffset:
                    if (!writer.TryWrite((IEnumerable<DateTimeOffset>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.TimeSpan:
                    if (!writer.TryWrite((IEnumerable<TimeSpan>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Guid:
                    if (!writer.TryWrite((IEnumerable<Guid>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;

                case CoreType.String:
                    if (!writer.TryWrite((IEnumerable<string>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;

                case CoreType.BooleanNullable:
                    if (!writer.TryWrite((IEnumerable<bool?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.ByteNullable:
                    if (!writer.TryWrite((IEnumerable<byte?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SByteNullable:
                    if (!writer.TryWrite((IEnumerable<sbyte?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int16Nullable:
                    if (!writer.TryWrite((IEnumerable<short?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt16Nullable:
                    if (!writer.TryWrite((IEnumerable<ushort?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int32Nullable:
                    if (!writer.TryWrite((IEnumerable<int?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt32Nullable:
                    if (!writer.TryWrite((IEnumerable<uint?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int64Nullable:
                    if (!writer.TryWrite((IEnumerable<long?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt64Nullable:
                    if (!writer.TryWrite((IEnumerable<ulong?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SingleNullable:
                    if (!writer.TryWrite((IEnumerable<float?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DoubleNullable:
                    if (!writer.TryWrite((IEnumerable<double?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DecimalNullable:
                    if (!writer.TryWrite((IEnumerable<decimal?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.CharNullable:
                    if (!writer.TryWrite((IEnumerable<char?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DateTimeNullable:
                    if (!writer.TryWrite((IEnumerable<DateTime?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.DateTimeOffsetNullable:
                    if (!writer.TryWrite((IEnumerable<DateTimeOffset?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.TimeSpanNullable:
                    if (!writer.TryWrite((IEnumerable<TimeSpan?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.GuidNullable:
                    if (!writer.TryWrite((IEnumerable<Guid?>)values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            state.EndFrame();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteEnumTypeEnumerable(ref ByteWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            typeDetail = typeDetail.InnerTypeDetail;

            var values = (IEnumerable)state.CurrentFrame.Object;

            int sizeNeeded;

            int length;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (typeDetail.TypeDetail.IsICollection)
                {
                    var collection = (ICollection)values;
                    length = collection.Count;
                }
                else if (typeDetail.TypeDetail.IsICollectionGeneric)
                {
                    length = (int)typeDetail.TypeDetail.GetMember("Count").Getter(values);
                }
                else
                {
                    var enumerable = (IEnumerable)values;
                    length = 0;
                    foreach (var item in enumerable)
                        length++;
                }

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.EnumerableLength = length;
            }
            else
            {
                length = state.CurrentFrame.EnumerableLength.Value;
            }

            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (typeDetail.TypeDetail.EnumUnderlyingType)
            {
                case CoreType.Byte:
                    if (!writer.TryWriteByteCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SByte:
                    if (!writer.TryWriteSByteCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int16:
                    if (!writer.TryWriteInt16Cast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt16:
                    if (!writer.TryWriteUInt16Cast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int32:
                    if (!writer.TryWriteInt32Cast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt32:
                    if (!writer.TryWriteUInt32Cast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int64:
                    if (!writer.TryWriteInt64Cast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt64:
                    if (!writer.TryWriteUInt64Cast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;

                case CoreType.ByteNullable:
                    if (!writer.TryWriteByteNullableCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.SByteNullable:
                    if (!writer.TryWriteSByteNullableCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int16Nullable:
                    if (!writer.TryWriteInt16NullableCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt16Nullable:
                    if (!writer.TryWriteUInt16NullableCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int32Nullable:
                    if (!writer.TryWriteInt32NullableCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt32Nullable:
                    if (!writer.TryWriteUInt32NullableCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.Int64Nullable:
                    if (!writer.TryWriteUInt64NullableCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                case CoreType.UInt64Nullable:
                    if (!writer.TryWriteUInt64NullableCast(values, length, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            state.EndFrame();
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static void WriteSpecialTypeEnumerable(ref ByteWriter writer, ref WriteState state)
        //{
        //    throw new NotImplementedException();
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteObjectEnumerable(ref ByteWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            var asList = !typeDetail.TypeDetail.Type.IsArray && typeDetail.TypeDetail.IsIList;

            var values = (IEnumerable)state.CurrentFrame.Object;

            int sizeNeeded;

            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                int length;
                if (typeDetail.TypeDetail.IsICollection)
                {
                    var collection = (ICollection)values;
                    length = collection.Count;
                }
                else if (typeDetail.TypeDetail.IsICollectionGeneric)
                {
                    length = (int)typeDetail.TypeDetail.GetMember("Count").Getter(values);
                }
                else
                {
                    var enumerable = (IEnumerable)values;
                    length = 0;
                    foreach (var item in enumerable)
                        length++;
                }

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.EnumerableLength = length;
            }

            typeDetail = typeDetail.InnerTypeDetail;

            state.CurrentFrame.ObjectEnumerator ??= values.GetEnumerator();

            while (state.CurrentFrame.EnumeratorObjectInProgress || state.CurrentFrame.ObjectEnumerator.MoveNext())
            {
                var value = state.CurrentFrame.ObjectEnumerator.Current;
                state.CurrentFrame.EnumeratorObjectInProgress = true;

                if (value == null)
                {
                    if (!writer.TryWriteNull(out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.EnumeratorObjectInProgress = false;
                    continue;
                }
                else
                {
                    if (!writer.TryWriteNotNull(out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                }

                state.CurrentFrame.EnumeratorObjectInProgress = false;
                var frame = WriteFrameFromType(ref state, value, typeDetail, false, false);
                state.PushFrame(frame);
                return;
            }

            state.EndFrame();
        }
    }
}