// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    public interface ICoreTypeSetter<T>
    {
        CoreType? CoreType { get; }
        bool IsByteArray { get; }

        void Setter(T model, bool value);
        void Setter(T model, byte value);
        void Setter(T model, sbyte value);
        void Setter(T model, short value);
        void Setter(T model, ushort value);
        void Setter(T model, int value);
        void Setter(T model, uint value);
        void Setter(T model, long value);
        void Setter(T model, ulong value);
        void Setter(T model, float value);
        void Setter(T model, double value);
        void Setter(T model, decimal value);
        void Setter(T model, char value);
        void Setter(T model, DateTime value);
        void Setter(T model, DateTimeOffset value);
        void Setter(T model, TimeSpan value);
        void Setter(T model, DateOnly value);
        void Setter(T model, TimeOnly value);
        void Setter(T model, Guid value);

        void Setter(T model, string value);

        void Setter(T model, bool? value);
        void Setter(T model, byte? value);
        void Setter(T model, sbyte? value);
        void Setter(T model, short? value);
        void Setter(T model, ushort? value);
        void Setter(T model, int? value);
        void Setter(T model, uint? value);
        void Setter(T model, long? value);
        void Setter(T model, ulong? value);
        void Setter(T model, float? value);
        void Setter(T model, double? value);
        void Setter(T model, decimal? value);
        void Setter(T model, char? value);
        void Setter(T model, DateTime? value);
        void Setter(T model, DateTimeOffset? value);
        void Setter(T model, TimeSpan? value);
        void Setter(T model, DateOnly? value);
        void Setter(T model, TimeOnly? value);
        void Setter(T model, Guid? value);

        void Setter(T model, byte[] value);
    }
}
