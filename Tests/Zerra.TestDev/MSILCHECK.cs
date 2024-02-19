// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


using System;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.TestDev
{
    public class MSILCHECK : ICoreTypeSetter<thing>
    {
        public CoreType? CoreType => Reflection.CoreType.Guid;

        public bool IsByteArray => false;

        public void Setter(thing model, bool value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, byte value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, sbyte value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, short value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, ushort value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, int value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, uint value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, long value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, ulong value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, float value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, double value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, decimal value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, char value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateTime value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateTimeOffset value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, TimeSpan value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateOnly value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, TimeOnly value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, Guid value)
        {
            model.ID = value;
        }

        public void Setter(thing model, string value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, bool? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, byte? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, sbyte? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, short? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, ushort? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, int? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, uint? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, long? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, ulong? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, float? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, double? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, decimal? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, char? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateTime? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateTimeOffset? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, TimeSpan? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateOnly? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, TimeOnly? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, Guid? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, byte[] value)
        {
            throw new NotImplementedException();
        }
    }
}
