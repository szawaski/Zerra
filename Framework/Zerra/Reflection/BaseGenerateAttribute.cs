﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Reflection
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class BaseGenerateAttribute : Attribute
    {
        public abstract Type? Generate(Type type);
    }
}
