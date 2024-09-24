// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;

namespace Zerra.Reflection.Runtime
{
    internal sealed class ParameterDetailRuntime : ParameterDetail
    {
        public override ParameterInfo ParameterInfo { get; }
        public override string Name => ParameterInfo.Name;
        public override Type Type => ParameterInfo.ParameterType;

        private readonly object locker;
        internal ParameterDetailRuntime(ParameterInfo parameter, object locker)
        {
            this.locker = locker;
            this.ParameterInfo = parameter;
        }

    }
}
