// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.State;
using Zerra.IO;

namespace Zerra.Serialization.Json.Converters
{
    public abstract class JsonConverter<TParent, TValue> : JsonConverter<TParent>,
        IJsonConverterHandles<TValue>
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

        public override sealed bool TryReadBoxed(ref CharReader reader, ref ReadState state, out object? returnValue)
        {
            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

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

            if (!ReadValueType(ref reader, ref state))
            {
                state.StashFrame();
                returnValue = default;
                return false;
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
        public override sealed bool TryWriteBoxed(ref CharWriter writer, ref WriteState state, object? value)
        {
            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
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

        public bool TryRead(ref CharReader reader, ref ReadState state, out TValue? returnValue)
        {
            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

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

            if (!ReadValueType(ref reader, ref state))
            {
                state.StashFrame();
                returnValue = default;
                return false;
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
        public bool TryWrite(ref CharWriter writer, ref WriteState state, TValue? value)
        {
            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
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

        public override sealed bool TryReadFromParent(ref CharReader reader, ref ReadState state, TParent? parent)
        {
            if (state.StackSize > maxStackDepth)
                throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

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

            if (!ReadValueType(ref reader, ref state))
            {
                state.StashFrame();
                return false;
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
        public override sealed bool TryWriteFromParent(ref CharWriter writer, ref WriteState state, TParent parent)
        {
            if (state.StackSize > maxStackDepth)
                throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");

            if (getter == null)
            {
                state.EndFrame();
                return true;
            }
            var value = getter(parent);

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
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
        public override sealed bool TryReadValueBoxed(ref CharReader reader, ref ReadState state, out object? value)
        {
            if (!ReadValueType(ref reader, ref state))
            {
                value = default;
                return false;
            }

            var read = TryReadValue(ref reader, ref state, out var v);
            value = v;
            return read;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWriteValueBoxed(ref CharWriter writer, ref WriteState state, object? value)
            => TryWriteValue(ref writer, ref state, (TValue?)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryReadValue(ref CharReader reader, ref ReadState state, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWriteValue(ref CharWriter writer, ref WriteState state, TValue? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CollectedValuesSetter(TParent? parent, object? value)
        {
            if (setter is not null && parent is not null && value is not null)
                setter(parent, (TValue)value);
        }
    }
}