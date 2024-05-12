// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public abstract class ConstructorDetail
    {
        public ConstructorInfo ConstructorInfo { get; }
        public string Name => ConstructorInfo.Name;

        private ParameterInfo[]? parameterInfos = null;
        public IReadOnlyList<ParameterInfo> ParametersInfo
        {
            get
            {
                if (this.parameterInfos == null)
                {
                    lock (locker)
                    {
                        this.parameterInfos ??= ConstructorInfo.GetParameters();
                    }
                }
                return this.parameterInfos;
            }
        }

        private Attribute[]? attributes = null;
        public IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (this.attributes == null)
                {
                    lock (locker)
                    {
                        this.attributes ??= ConstructorInfo.GetCustomAttributes().ToArray();
                    }
                }
                return this.attributes;
            }
        }

        private bool creatorLoaded = false;
        private Func<object?[]?, object>? creator = null;
        public Func<object?[]?, object> Creator
        {
            get
            {
                LoadCreator();
                return this.creator ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(Creator)}"); ;
            }
        }
        public bool HasCreator
        {
            get
            {
                LoadCreator();
                return this.creator != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreator()
        {
            if (!creatorLoaded)
            {
                lock (locker)
                {
                    if (!creatorLoaded)
                    {
                        if (ConstructorInfo.DeclaringType != null && !ConstructorInfo.DeclaringType.IsAbstract && !ConstructorInfo.DeclaringType.IsGenericTypeDefinition)
                        {
                            this.creator = AccessorGenerator.GenerateCreator(ConstructorInfo);
                        }
                        creatorLoaded = true;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"new({(String.Join(", ", ParametersInfo.Select(x => $"{x.ParameterType.Name} {x.Name}").ToArray()))})";
        }

        protected readonly object locker;
        protected ConstructorDetail(ConstructorInfo constructor, object locker)
        {
            this.locker = locker;
            this.ConstructorInfo = constructor;
        }

        private static readonly Type typeDetailT = typeof(ConstructorDetail<>);
        internal static ConstructorDetail New(Type type, ConstructorInfo constructor, object locker)
        {
            var typeDetailGeneric = typeDetailT.MakeGenericType(type);
            var obj = typeDetailGeneric.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object[] { constructor, locker });
            return (ConstructorDetail)obj;
        }
    }
}
