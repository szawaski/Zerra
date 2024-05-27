// Copyright © KaKush LLC
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
        private Func<TParent, object?>? getterObject;
        private Action<TParent, object?>? setterObject;

        private bool useEmptyImplementation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Setup(TypeDetail? typeDetail, string? memberKey, Delegate? getterBoxed, Delegate? setterBoxed)
        {
            if (typeDetail == null)
                throw new ArgumentNullException(nameof(typeDetail));

            this.typeDetail = (TypeDetail<TValue>)typeDetail;
            this.memberKey = memberKey;
            this.getter = (Func<TParent, TValue?>?)getterBoxed;
            this.setter = (Action<TParent, TValue?>?)setterBoxed;
            if (this.getter != null)
                this.getterObject = (parent) => (object?)getter(parent);

            this.useEmptyImplementation = typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric;

            Setup();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryRead(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (state.Stack.Count > maxStackDepth)
                return false;

            if ((state.IncludePropertyTypes || useEmptyImplementation) && !state.Current.HasTypeRead)
            {
                if (state.IncludePropertyTypes)
                {
                    int sizeNeeded;
                    if (!state.Current.StringLength.HasValue)
                    {
                        if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return false;
                        }
                        state.Current.StringLength = stringLength;
                    }

                    if (!reader.TryReadString(state.Current.StringLength!.Value, out var typeName, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }

                    if (typeName == null)
                        throw new NotSupportedException("Cannot deserialize without type information");

                    var typeFromBytes = Discovery.GetTypeFromName(typeName);

                    var newTypeDetail = typeFromBytes.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if (newTypeDetail.Type != typeDetail.Type && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);
                    state.Current.StringLength = null;
                    state.Current.Converter = newConverter;
                    state.Current.HasTypeRead = true;
                    return newConverter.TryRead(ref reader, ref state, parent);
                }

                if (useEmptyImplementation)
                {
                    var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                    var newTypeDetail = emptyImplementationType.GetTypeDetail();

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);
                    state.Current.Converter = newConverter;
                    state.Current.HasTypeRead = true;
                    return newConverter.TryRead(ref reader, ref state, parent);
                }

                state.Current.HasTypeRead = true;
            }

            if (!TryRead(ref reader, ref state, out var value))
                return false;

            if (setter != null && parent != null)
                setter(parent, value);

            state.EndFrame(value);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWrite(ref ByteWriter writer, ref WriteState state, TParent? parent)
        {
            if (state.Stack.Count > maxStackDepth)
                return false;

            TValue? value;
            if (parent != null)
            {
                if (getter == null)
                {
                    state.EndFrame();
                    return true;
                }
                value = getter(parent);
            }
            else
            {
                value = (TValue?)state.Object;
            }

            if ((state.IncludePropertyTypes || useEmptyImplementation) && !state.Current.HasTypeWritten)
            {
                if (state.IncludePropertyTypes)
                {
                    var typeFromValue = value == null ? typeDetail : value.GetType().GetTypeDetail();
                    var typeName = typeFromValue.Type.FullName;

                    if (!writer.TryWrite(typeName, false, out var sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }

                    if (typeFromValue.Type != typeDetail.Type)
                    {
                        var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                        state.Current.Converter = newConverter;
                        state.Current.HasTypeWritten = true;
                        return newConverter.TryWrite(ref writer, ref state, value);
                    }
                }
                else if (useEmptyImplementation)
                {
                    var typeFromValue = value == null ? typeDetail : value.GetType().GetTypeDetail();
                    var typeName = typeFromValue.Type.FullName;

                    if (typeFromValue.Type != typeDetail.Type)
                    {
                        var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                        state.Current.Converter = newConverter;
                        state.Current.HasTypeWritten = true;
                        return newConverter.TryWrite(ref writer, ref state, parent);
                    }
                }

                state.Current.HasTypeWritten = true;
            }

            if (!TryWrite(ref writer, ref state, value))
                return false;

            state.EndFrame();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Setup() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryRead(ref ByteReader reader, ref ReadState state, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWrite(ref ByteWriter writer, ref WriteState state, TValue? value);
    }
}