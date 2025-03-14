﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;

namespace Zerra.Reflection
{
    public abstract class ParameterDetail
    {
        public abstract bool IsGenerated { get; }

        public abstract ParameterInfo ParameterInfo { get; }
        public abstract string? Name { get; }
        public abstract Type Type { get; }

        public override string ToString()
        {
            return $"{(IsGenerated ? "Generated" : "Runtime")} {Type.Name} {Name}";
        }
    }
}
