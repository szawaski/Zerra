﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Zerra.Serialization
{
    public abstract class ByteConverter<TParent, TValue> : ByteConverter<TParent>,
        IByteConverterHandles<TValue>
    {
        protected TypeDetail<TValue> typeDetail { get; private set; } = null!;
        private string? memberKey;
        private Func<TParent, TValue?>? getter;
        private Action<TParent, TValue?>? setter;

        private bool useEmptyImplementation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Setup(TypeDetail? typeDetail, string? memberKey, Delegate? getterDelegate, Delegate? setterDelegate)
        {
            if (typeDetail == null)
                throw new ArgumentNullException(nameof(typeDetail));

            this.typeDetail = (TypeDetail<TValue>)typeDetail;
            this.memberKey = memberKey;
            if (getterDelegate != null)
            {
                this.getter = getterDelegate as Func<TParent, TValue?>;
                this.getter ??= (parent) => (TValue?)getterDelegate.DynamicInvoke(parent);
            }
            if (setterDelegate != null)
            {
                this.setter = setterDelegate as Action<TParent, TValue?>;
                this.setter ??= (parent, value) => setterDelegate.DynamicInvoke(parent, value);
            }

            this.useEmptyImplementation = typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric;

            Setup();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Setup() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryRead(ref ByteReader reader, ref ReadState state, out object? returnValue)
        {
            if (state.IncludePropertyTypes)
            {
                Type? typeFromBytes;
                if (state.Current.ReadType == null)
                {
                    int sizeNeeded;
                    if (!state.Current.StringLength.HasValue)
                    {
                        if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            state.StashFrame();
                            returnValue = default;
                            return false;
                        }
                        state.Current.StringLength = stringLength;
                    }

                    if (!reader.TryReadString(state.Current.StringLength!.Value, out var typeName, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        state.StashFrame();
                        returnValue = default;
                        return false;
                    }

                    if (typeName == null)
                        throw new NotSupportedException("Cannot deserialize without type information");

                    state.Current.StringLength = null;
                    typeFromBytes = Discovery.GetTypeFromName(typeName);
                }
                else
                {
                    typeFromBytes = state.Current.ReadType;
                }

                if (typeFromBytes != typeDetail.Type)
                {
                    var newTypeDetail = typeFromBytes.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if (!newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                    if (!newConverter.TryReadValueObject(ref reader, ref state, out var valueObject))
                    {
                        state.Current.ReadType = typeFromBytes;
                        state.StashFrame();
                        returnValue = default;
                        return false;
                    }

                    state.EndFrame();
                    returnValue = valueObject;
                    return true;
                }
            }

            if (useEmptyImplementation)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueObject(ref reader, ref state, out var valueObject))
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
                returnValue = default;
                return false;
            }

            state.EndFrame();
            returnValue = value;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWrite(ref ByteWriter writer, ref WriteState state, object? value)
        {
            if (state.IncludePropertyTypes)
            {
                Type typeFromValue;
                if (state.Current.WriteType == null)
                {
                    typeFromValue = value == null ? typeDetail.Type : value.GetType();
                    var typeName = typeFromValue.FullName;

                    if (!writer.TryWrite(typeName, false, out var sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        state.StashFrame();
                        return false;
                    }
                }
                else
                {
                    typeFromValue = state.Current.WriteType;
                }

                if (typeFromValue != typeDetail.Type)
                {
                    var typeFromValueDetail = typeFromValue.GetTypeDetail();
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValueDetail, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueObject(ref writer, ref state, value))
                    {
                        state.Current.WriteType = typeFromValue;
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (useEmptyImplementation)
            {
                var typeFromValue = value == null ? typeDetail : value.GetType().GetTypeDetail();
                var typeName = typeFromValue.Type.FullName;

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueObject(ref writer, ref state, value))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryReadFromParent(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (state.StackSize > maxStackDepth)
                throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");

            if (state.IncludePropertyTypes)
            {
                Type? typeFromBytes;
                if (state.Current.ReadType == null)
                {
                    int sizeNeeded;
                    if (!state.Current.StringLength.HasValue)
                    {
                        if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            state.StashFrame();
                            return false;
                        }
                        state.Current.StringLength = stringLength;
                    }

                    if (!reader.TryReadString(state.Current.StringLength!.Value, out var typeName, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        state.StashFrame();
                        return false;
                    }

                    if (typeName == null)
                        throw new NotSupportedException("Cannot deserialize without type information");

                    state.Current.StringLength = null;
                    typeFromBytes = Discovery.GetTypeFromName(typeName);
                }
                else
                {
                    typeFromBytes = state.Current.ReadType;
                }

                if (typeFromBytes != typeDetail.Type)
                {
                    var newTypeDetail = typeFromBytes.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if (!newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                    if (!newConverter.TryReadValueObject(ref reader, ref state, out var valueObject))
                    {
                        state.Current.ReadType = typeFromBytes;
                        state.StashFrame();
                        return false;
                    }

                    if (setter != null && parent != null)
                        setter(parent, (TValue?)valueObject);
                    state.EndFrame();
                    return true;
                }
            }

            if (useEmptyImplementation)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueObject(ref reader, ref state, out var valueObject))
                {
                    state.StashFrame();
                    return false;
                }

                if (setter != null && parent != null)
                    setter(parent, (TValue?)valueObject);
                state.EndFrame();
                return true;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                state.StashFrame();
                return false;
            }

            if (setter != null && parent != null)
                setter(parent, value);
            state.EndFrame();
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            if (state.IncludePropertyTypes)
            {
                Type typeFromValue;
                if (state.Current.WriteType == null)
                {
                    typeFromValue = value == null ? typeDetail.Type : value.GetType();
                    var typeName = typeFromValue.FullName;

                    if (!writer.TryWrite(typeName, false, out var sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        state.StashFrame();
                        return false;
                    }
                }
                else
                {
                    typeFromValue = state.Current.WriteType;
                }

                if (typeFromValue != typeDetail.Type)
                {
                    var typeFromValueDetail = typeFromValue.GetTypeDetail();
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValueDetail, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueObject(ref writer, ref state, value))
                    {
                        state.Current.WriteType = typeFromValue;
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (useEmptyImplementation)
            {
                var typeFromValue = value == null ? typeDetail : value.GetType().GetTypeDetail();
                var typeName = typeFromValue.Type.FullName;

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueObject(ref writer, ref state, value))
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
        public override bool TryReadValueObject(ref ByteReader reader, ref ReadState state, out object? value)
        {
            var read = TryReadValue(ref reader, ref state, out var v);
            value = v;
            return read;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryWriteValueObject(ref ByteWriter writer, ref WriteState state, object? value)
            => TryWriteValue(ref writer, ref state, (TValue?)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TValue? value);
    }
}