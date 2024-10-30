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
        protected virtual bool StackRequired { get; } = true;

        protected TypeDetail<TValue> typeDetail { get; private set; } = null!;
        private string memberKey = null!;
        private Func<TParent, TValue?>? getter;
        private Action<TParent, TValue?>? setter;

        private bool canBeNull;
        private bool isInterfacedObject;

        public override void Setup(TypeDetail typeDetail, string memberKey, Delegate? getterDelegate, Delegate? setterDelegate)
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

            canBeNull = typeDetail.IsNullable || !typeDetail.Type.IsValueType;
            isInterfacedObject = typeDetail.Type.IsInterface && !typeDetail.HasIEnumerableGeneric && !typeDetail.HasIEnumerable;

            Setup();
        }

        protected virtual void Setup() { }

        public override sealed bool TryReadBoxed(ref ByteReader reader, ref ReadState state, out object? returnValue)
        {
            if (canBeNull && !state.EntryHasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out state.SizeNeeded))
                {
                    returnValue = default;
                    return false;
                }

                if (isNull)
                {
                    returnValue = default;
                    return true;
                }
            }

            if (state.UseTypes)
            {
                if (state.EntryReadType is null)
                {
                    if (!reader.TryRead(out string? typeName, out state.SizeNeeded))
                    {
                        state.EntryHasNullChecked = true;
                        returnValue = default;
                        return false;
                    }

                    if (typeName is null)
                        throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} without type information");

                    state.EntryReadType = Discovery.GetTypeFromName(typeName);
                }

                if (state.EntryReadType != typeDetail.Type)
                {
                    var newTypeDetail = state.EntryReadType.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if ((!typeDetail.IsNullable || typeDetail.InnerType != newTypeDetail.Type) && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                    if (StackRequired)
                    {
                        if (state.StackSize >= maxStackDepth)
                            throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                        state.PushFrame(false);
                    }

                    if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        state.EntryHasNullChecked = true;
                        returnValue = default;
                        return false;
                    }

                    if (StackRequired)
                        state.EndFrame();
                    returnValue = valueObject;
                    return true;
                }
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(false);
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    state.EntryHasNullChecked = true;
                    returnValue = default;
                    return false;
                }

                if (StackRequired)
                    state.EndFrame();
                returnValue = valueObject;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                if (StackRequired)
                    state.StashFrame();
                state.EntryHasNullChecked = true;
                returnValue = value;
                return false;
            }

            if (StackRequired)
                state.EndFrame();
            returnValue = value;
            return true;
        }
        public override sealed bool TryWriteBoxed(ref ByteWriter writer, ref WriteState state, in object? value)
        {
            if (canBeNull)
            {
                if (value is null)
                {
                    if (!state.EntryHasWrittenIsNull)
                    {
                        if (!writer.TryWriteNull(out state.BytesNeeded))
                            return false;
                    }
                    return true;
                }
                else
                {
                    if (!state.EntryHasWrittenIsNull)
                    {
                        if (!writer.TryWriteNotNull(out state.BytesNeeded))
                            return false;
                    }
                }
            }

            if (state.UseTypes)
            {
                if (state.EntryWriteType is null)
                {
                    var writeType = value!.GetType();
                    var typeName = writeType.AssemblyQualifiedName ?? throw new InvalidOperationException($"Type {writeType} does not have {nameof(writeType.AssemblyQualifiedName)}");

                    if (!writer.TryWrite(typeName, out state.BytesNeeded))
                    {
                        state.EntryHasWrittenIsNull = true;
                        return false;
                    }

                    state.EntryWriteType = writeType;
                }

                if (state.EntryWriteType != typeDetail.Type)
                {
                    var typeFromValueDetail = state.EntryWriteType.GetTypeDetail();
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValueDetail, memberKey, getter, setter);

                    if (StackRequired)
                    {
                        if (state.StackSize >= maxStackDepth)
                            throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                        state.PushFrame();
                    }

                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        state.EntryHasWrittenIsNull = true;
                        return false;
                    }

                    if (StackRequired)
                        state.EndFrame();
                    return true;
                }
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame();
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value!.GetType();

                if (typeFromValue != typeDetail.Type)
                {
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue.GetTypeDetail(), memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        state.EntryHasWrittenIsNull = true;
                        return false;
                    }
                    if (StackRequired)
                        state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, (TValue)value!))
            {
                if (StackRequired)
                    state.StashFrame();
                state.EntryHasWrittenIsNull = true;
                return false;
            }
            if (StackRequired)
                state.EndFrame();
            return true;
        }

        public bool TryRead(ref ByteReader reader, ref ReadState state, out TValue? returnValue)
        {
            if (canBeNull && !state.EntryHasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out state.SizeNeeded))
                {
                    returnValue = default;
                    return false;
                }

                if (isNull)
                {
                    returnValue = default;
                    return true;
                }
            }

            if (state.UseTypes)
            {
                if (state.EntryReadType is null)
                {
                    if (!reader.TryRead(out string? typeName, out state.SizeNeeded))
                    {
                        state.EntryHasNullChecked = true;
                        returnValue = default;
                        return false;
                    }

                    if (typeName is null)
                        throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} without type information");

                    state.EntryReadType = Discovery.GetTypeFromName(typeName);
                }

                if (state.EntryReadType != typeDetail.Type)
                {
                    var newTypeDetail = state.EntryReadType.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if ((!typeDetail.IsNullable || typeDetail.InnerType != newTypeDetail.Type) && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                    if (StackRequired)
                    {
                        if (state.StackSize >= maxStackDepth)
                            throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                        state.PushFrame(false);
                    }

                    if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        state.EntryHasNullChecked = true;
                        returnValue = default;
                        return false;
                    }

                    if (StackRequired)
                        state.EndFrame();
                    returnValue = (TValue?)valueObject;
                    return true;
                }
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(false);
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    state.EntryHasNullChecked = true;
                    returnValue = default;
                    return false;
                }

                if (StackRequired)
                    state.EndFrame();
                returnValue = (TValue?)valueObject;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                if (StackRequired)
                    state.StashFrame();
                state.EntryHasNullChecked = true;
                returnValue = value;
                return false;
            }

            if (StackRequired)
                state.EndFrame();
            returnValue = value;
            return true;
        }
        public bool TryWrite(ref ByteWriter writer, ref WriteState state, in TValue? value)
        {
            if (canBeNull)
            {
                if (value is null)
                {
                    if (!state.EntryHasWrittenIsNull)
                    {
                        if (!writer.TryWriteNull(out state.BytesNeeded))
                            return false;
                    }
                    return true;
                }
                else
                {
                    if (!state.EntryHasWrittenIsNull)
                    {
                        if (!writer.TryWriteNotNull(out state.BytesNeeded))
                            return false;
                    }
                }
            }

            if (state.UseTypes)
            {
                if (state.EntryWriteType is null)
                {
                    var writeType = value!.GetType();
                    var typeName = writeType.AssemblyQualifiedName ?? throw new InvalidOperationException($"Type {writeType} does not have {nameof(writeType.AssemblyQualifiedName)}");

                    if (!writer.TryWrite(typeName, out state.BytesNeeded))
                    {
                        state.EntryHasWrittenIsNull = true;
                        return false;
                    }

                    state.EntryWriteType = writeType;
                }

                if (state.EntryWriteType != typeDetail.Type)
                {
                    var typeFromValueDetail = state.EntryWriteType.GetTypeDetail();
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValueDetail, memberKey, getter, setter);

                    if (StackRequired)
                    {
                        if (state.StackSize >= maxStackDepth)
                            throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                        state.PushFrame();
                    }

                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        state.EntryHasWrittenIsNull = true;
                        return false;
                    }
                    if (StackRequired)
                        state.EndFrame();
                    return true;
                }
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame();
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value!.GetType();

                if (typeFromValue != typeDetail.Type)
                {
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue.GetTypeDetail(), memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        state.EntryHasWrittenIsNull = true;
                        return false;
                    }
                    if (StackRequired)
                        state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, value!))
            {
                if (StackRequired)
                    state.StashFrame();
                state.EntryHasWrittenIsNull = true;
                return false;
            }
            if (StackRequired)
                state.EndFrame();
            return true;
        }

        public override sealed bool TryReadFromParent(ref ByteReader reader, ref ReadState state, TParent? parent, bool nullFlags, bool drainBytes)
        {
            if (nullFlags && canBeNull && !state.Current.ChildHasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out state.SizeNeeded))
                    return false;

                if (isNull)
                {
                    if (setter is not null && parent is not null)
                        setter(parent, default);
                    return true;
                }
            }

            if (state.UseTypes)
            {
                if (state.Current.ChildReadType is null)
                {
                    if (!reader.TryRead(out string? typeName, out state.SizeNeeded))
                    {
                        state.Current.ChildHasNullChecked = true;
                        return false;
                    }

                    if (typeName is null)
                        throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} without type information");

                    state.Current.ChildReadType = Discovery.GetTypeFromName(typeName);
                }

                if (state.Current.ChildReadType != typeDetail.Type)
                {
                    var newTypeDetail = state.Current.ChildReadType.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if ((!typeDetail.IsNullable || typeDetail.InnerType != newTypeDetail.Type) && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");

                    var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                    if (StackRequired)
                    {
                        if (state.StackSize >= maxStackDepth)
                            throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                        state.PushFrame(drainBytes);
                    }

                    if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        return false;
                    }

                    if (setter is not null && parent is not null)
                        setter(parent, (TValue?)valueObject);
                    if (StackRequired)
                        state.EndFrame();
                    state.Current.ChildReadType = null;
                    state.Current.ChildHasNullChecked = false;
                    return true;
                }
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(drainBytes);
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    state.Current.ChildHasNullChecked = true;
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                if (StackRequired)
                    state.EndFrame();
                if (state.UseTypes)
                    state.Current.ChildReadType = null;
                state.Current.ChildHasNullChecked = false;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                if (StackRequired)
                    state.StashFrame();
                state.Current.ChildHasNullChecked = true;
                return false;
            }

            if (setter is not null && parent is not null)
                setter(parent, value);
            if (StackRequired)
                state.EndFrame();
            if (state.UseTypes)
                state.Current.ChildReadType = null;
            state.Current.ChildHasNullChecked = false;
            return true;
        }
        public override sealed bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, TParent parent, bool nullFlags, ushort indexProperty, ReadOnlySpan<byte> indexPropertyName)
        {
            if (getter is null)
                return true;
            var value = getter(parent);

            if (canBeNull && !state.Current.ChildHasWrittenIsNull)
            {
                if (value is null)
                {
                    if (nullFlags)
                    {
                        if (!writer.TryWriteNull(out state.BytesNeeded))
                            return false;
                    }
                    return true;
                }
                else
                {
                    if (nullFlags)
                    {
                        if (!writer.TryWriteNotNull(out state.BytesNeeded))
                            return false;
                    }
                }
            }

            if (!state.Current.HasWrittenPropertyIndex)
            {
                if (state.UseMemberNames)
                {
                    if (indexPropertyName.Length > 0)
                    {
                        if (!writer.TryWritePropertyName(indexPropertyName, out state.BytesNeeded))
                        {
                            state.Current.ChildHasWrittenIsNull = true;
                            return false;
                        }
                    }
                }
                else
                {
                    if (indexProperty > 0)
                    {
                        if (state.UseIndexSizeUInt16)
                        {
                            if (!writer.TryWrite(indexProperty, out state.BytesNeeded))
                            {
                                state.Current.ChildHasWrittenIsNull = true;
                                return false;
                            }
                        }
                        else
                        {
                            if (!writer.TryWrite((byte)indexProperty, out state.BytesNeeded))
                            {
                                state.Current.ChildHasWrittenIsNull = true;
                                return false;
                            }
                        }
                    }
                }
            }

            if (state.UseTypes)
            {
                if (state.Current.ChildWriteType is null)
                {
                    var writeType = value!.GetType();
                    var typeName = writeType.AssemblyQualifiedName ?? throw new InvalidOperationException($"Type {writeType} does not have {nameof(writeType.AssemblyQualifiedName)}");

                    if (!writer.TryWrite(typeName, out state.BytesNeeded))
                    {
                        state.Current.HasWrittenPropertyIndex = true;
                        state.Current.ChildHasWrittenIsNull = true;
                        return false;
                    }

                    state.Current.ChildWriteType = writeType;
                }

                if (state.Current.ChildWriteType != typeDetail.Type)
                {
                    var typeFromValueDetail = state.Current.ChildWriteType.GetTypeDetail();
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValueDetail, memberKey, getter, setter);

                    if (StackRequired)
                    {
                        if (state.StackSize >= maxStackDepth)
                            throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                        state.PushFrame();
                    }

                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        state.Current.HasWrittenPropertyIndex = true;
                        state.Current.ChildHasWrittenIsNull = true;
                        return false;
                    }

                    if (StackRequired)
                        state.EndFrame();
                    state.Current.HasWrittenPropertyIndex = false;
                    state.Current.ChildWriteType = null;
                    state.Current.ChildHasWrittenIsNull = false;
                    return true;
                }
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(ByteConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame();
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value!.GetType();

                if (typeFromValue != typeDetail.Type)
                {
                    var newConverter = ByteConverterFactory<TParent>.Get(typeFromValue.GetTypeDetail(), memberKey, getter, setter);

                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        state.Current.HasWrittenPropertyIndex = true;
                        state.Current.ChildHasWrittenIsNull = true;
                        return false;
                    }

                    if (StackRequired)
                        state.EndFrame();
                    state.Current.HasWrittenPropertyIndex = false;
                    if (state.UseTypes)
                        state.Current.ChildWriteType = null;
                    state.Current.ChildHasWrittenIsNull = false;
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, value!))
            {
                if (StackRequired)
                    state.StashFrame();
                state.Current.HasWrittenPropertyIndex = true;
                state.Current.ChildHasWrittenIsNull = true;
                return false;
            }

            if (StackRequired)
                state.EndFrame();
            state.Current.HasWrittenPropertyIndex = false;
            if (state.UseTypes)
                state.Current.ChildWriteType = null;
            state.Current.ChildHasWrittenIsNull = false;
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
        public override sealed bool TryWriteValueBoxed(ref ByteWriter writer, ref WriteState state, in object value)
            => TryWriteValue(ref writer, ref state, (TValue)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TValue value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void CollectedValuesSetter(TParent? parent, in object? value)
        {
            if (setter is not null && parent is not null && value is not null)
                setter(parent, (TValue)value);
        }
    }
}