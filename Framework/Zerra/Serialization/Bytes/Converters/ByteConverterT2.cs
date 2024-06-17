// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
using Zerra.Serialization.Bytes.State;
using Zerra.Serialization.Bytes.IO;

namespace Zerra.Serialization.Bytes.Converters
{
    public abstract class ByteConverter<TParent, TValue> : ByteConverter<TParent>,
        IByteConverterHandles<TValue>
    {
        protected TypeDetail<TValue> typeDetail { get; private set; } = null!;
        private string? memberKey;
        private Func<TParent, TValue?>? getter;
        private Action<TParent, TValue?>? setter;

        private bool isInterfacedObject;

        public override void Setup(TypeDetail typeDetail, string? memberKey, Delegate? getterDelegate, Delegate? setterDelegate)
        {
            this.typeDetail = (TypeDetail<TValue>)typeDetail;
            this.memberKey = memberKey;
            if (getterDelegate is not null)
            {
                getter = getterDelegate as Func<TParent, TValue?>;
                getter ??= (parent) => (TValue?)getterDelegate.DynamicInvoke(parent);
            }
            if (setterDelegate is not null)
            {
                setter = setterDelegate as Action<TParent, TValue?>;
                setter ??= (parent, value) => setterDelegate.DynamicInvoke(parent, value);
            }

            isInterfacedObject = typeDetail.Type.IsInterface && !typeDetail.HasIEnumerableGeneric && !typeDetail.HasIEnumerable;

            Setup();
        }

        protected virtual void Setup() { }

        public override sealed bool TryReadBoxed(ref ByteReader reader, ref ReadState state, out object? returnValue)
        {
            if (state.UseTypes)
            {
                Type? typeFromBytes;
                if (state.Current.ReadType == null)
                {
                    if (!reader.TryRead(false, out string? typeName, out state.BytesNeeded))
                    {
                        state.StashFrame();
                        returnValue = default;
                        return false;
                    }

                    if (typeName == null)
                        throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} without type information");

                    typeFromBytes = Discovery.GetTypeFromName(typeName);

                    state.Current.ReadType = typeFromBytes;
                }
                else
                {
                    typeFromBytes = state.Current.ReadType;
                }

                if (typeFromBytes != typeDetail.Type)
                {
                    var newTypeDetail = typeFromBytes.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if ((typeDetail.IsNullable && typeDetail.InnerType != newTypeDetail.Type) && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                    if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                    {
                        state.StashFrame();
                        returnValue = default;
                        return false;
                    }

                    state.EndFrame();
                    returnValue = valueObject;
                    return true;
                }
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    state.StashFrame();
                    returnValue = default;
                    return false;
                }

                state.EndFrame();
                returnValue = valueObject;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                state.StashFrame();
                returnValue = value;
                return false;
            }

            state.EndFrame();
            returnValue = value;
            return true;
        }
        public override sealed bool TryWriteBoxed(ref ByteWriter writer, ref WriteState state, object? value)
        {
            if (state.IncludeTypes)
            {
                Type typeFromValue;
                if (state.Current.WriteType == null)
                {
                    typeFromValue = value is null ? typeDetail.Type : value.GetType();
                    var typeName = typeFromValue.AssemblyQualifiedName;

                    if (!writer.TryWrite(typeName, false, out state.BytesNeeded))
                    {
                        state.StashFrame();
                        return false;
                    }

                    state.Current.WriteType = typeFromValue;
                }
                else
                {
                    typeFromValue = state.Current.WriteType;
                }

                if (typeFromValue != typeDetail.Type)
                {
                    var typeFromValueDetail = typeFromValue.GetTypeDetail();
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValueDetail, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, (TValue?)value))
            {
                state.StashFrame();
                return false;
            }
            state.EndFrame();
            return true;
        }

        public bool TryRead(ref ByteReader reader, ref ReadState state, out TValue? returnValue)
        {
            if (state.UseTypes)
            {
                Type? typeFromBytes;
                if (state.Current.ReadType == null)
                {
                    if (!reader.TryRead(false, out string? typeName, out state.BytesNeeded))
                    {
                        state.StashFrame();
                        returnValue = default;
                        return false;
                    }

                    if (typeName == null)
                        throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} without type information");

                    typeFromBytes = Discovery.GetTypeFromName(typeName);

                    state.Current.ReadType = typeFromBytes;
                }
                else
                {
                    typeFromBytes = state.Current.ReadType;
                }

                if (typeFromBytes != typeDetail.Type)
                {
                    var newTypeDetail = typeFromBytes.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if ((typeDetail.IsNullable && typeDetail.InnerType != newTypeDetail.Type) && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                    if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                    {
                        state.StashFrame();
                        returnValue = default;
                        return false;
                    }

                    state.EndFrame();
                    returnValue = (TValue?)valueObject;
                    return true;
                }
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    state.StashFrame();
                    returnValue = default;
                    return false;
                }

                state.EndFrame();
                returnValue = (TValue?)valueObject;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                state.StashFrame();
                returnValue = value;
                return false;
            }

            state.EndFrame();
            returnValue = value;
            return true;
        }
        public bool TryWrite(ref ByteWriter writer, ref WriteState state, TValue? value)
        {
            if (state.IncludeTypes)
            {
                Type typeFromValue;
                if (state.Current.WriteType == null)
                {
                    typeFromValue = value is null ? typeDetail.Type : value.GetType();
                    var typeName = typeFromValue.AssemblyQualifiedName;

                    if (!writer.TryWrite(typeName, false, out state.BytesNeeded))
                    {
                        state.StashFrame();
                        return false;
                    }

                    state.Current.WriteType = typeFromValue;
                }
                else
                {
                    typeFromValue = state.Current.WriteType;
                }

                if (typeFromValue != typeDetail.Type)
                {
                    var typeFromValueDetail = typeFromValue.GetTypeDetail();
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValueDetail, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, value))
            {
                state.StashFrame();
                return false;
            }
            state.EndFrame();
            return true;
        }

        public override sealed bool TryReadFromParent(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (state.StackSize > maxStackDepth)
                throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");

            if (state.UseTypes)
            {
                Type? typeFromBytes;
                if (state.Current.ReadType == null)
                {
                    if (!reader.TryRead(false, out string? typeName, out state.BytesNeeded))
                    {
                        state.StashFrame();
                        return false;
                    }

                    if (typeName == null)
                        throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} without type information");

                    typeFromBytes = Discovery.GetTypeFromName(typeName);

                    state.Current.ReadType = typeFromBytes;
                }
                else
                {
                    typeFromBytes = state.Current.ReadType;
                }

                if (typeFromBytes != typeDetail.Type)
                {
                    var newTypeDetail = typeFromBytes.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if ((typeDetail.IsNullable && typeDetail.InnerType != newTypeDetail.Type) && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                    if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                    {
                        state.StashFrame();
                        return false;
                    }

                    if (setter is not null && parent is not null)
                        setter(parent, (TValue?)valueObject);
                    state.EndFrame();
                    return true;
                }
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    state.StashFrame();
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                state.EndFrame();
                return true;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                state.StashFrame();
                return false;
            }

            if (setter is not null && parent is not null)
                setter(parent, value);
            state.EndFrame();
            return true;
        }
        public override sealed bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, TParent parent)
        {
            if (state.StackSize > maxStackDepth)
                throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");

            if (getter == null)
            {
                state.EndFrame();
                return true;
            }
            var value = getter(parent);

            if (!state.UsePropertyNames && state.Current.IndexProperty > 0 || state.UsePropertyNames && state.Current.IndexPropertyName is not null)
            {
                if (value is null)
                {
                    state.EndFrame();
                    return true;
                }

                if (!state.Current.HasWrittenPropertyIndex)
                {
                    if (state.UsePropertyNames)
                    {
                        if (!writer.TryWrite(state.Current.IndexPropertyName, false, out state.BytesNeeded))
                        {
                            state.StashFrame();
                            return false;
                        }
                    }
                    else
                    {
                        if (state.IndexSizeUInt16)
                        {
                            if (!writer.TryWrite(state.Current.IndexProperty, out state.BytesNeeded))
                            {
                                state.StashFrame();
                                return false;
                            }
                        }
                        else
                        {
                            if (!writer.TryWrite((byte)state.Current.IndexProperty, out state.BytesNeeded))
                            {
                                state.StashFrame();
                                return false;
                            }
                        }
                    }
                    state.Current.HasWrittenPropertyIndex = true;
                }
            }

            if (state.IncludeTypes)
            {
                Type typeFromValue;
                if (state.Current.WriteType == null)
                {
                    typeFromValue = value is null ? typeDetail.Type : value.GetType();
                    var typeName = typeFromValue.AssemblyQualifiedName;

                    if (!writer.TryWrite(typeName, false, out state.BytesNeeded))
                    {
                        state.StashFrame();
                        return false;
                    }

                    state.Current.WriteType = typeFromValue;
                }
                else
                {
                    typeFromValue = state.Current.WriteType;
                }

                if (typeFromValue != typeDetail.Type)
                {
                    var typeFromValueDetail = typeFromValue.GetTypeDetail();
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValueDetail, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, value))
            {
                state.StashFrame();
                return false;
            }
            state.EndFrame();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryReadValueBoxed(ref ByteReader reader, ref ReadState state, out object? value)
        {
            var read = TryReadValue(ref reader, ref state, out var v);
            value = v;
            return read;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWriteValueBoxed(ref ByteWriter writer, ref WriteState state, object? value)
            => TryWriteValue(ref writer, ref state, (TValue?)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TValue? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CollectedValuesSetter(TParent? parent, object? value)
        {
            if (setter is not null && parent is not null && value is not null)
                setter(parent, (TValue)value);
        }
    }
}