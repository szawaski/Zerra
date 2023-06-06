// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zerra.Reflection
{
    public sealed class ConstructorDetails
    {
        public ConstructorInfo ConstructorInfo { get; private set; }
        public string Name { get; private set; }

        private ParameterInfo[] parameterInfos = null;
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

        private Attribute[] attributes = null;
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
        private Func<object[], object> creator = null;
        public Func<object[], object> Creator
        {
            get
            {
                if (!creatorLoaded)
                {
                    lock (locker)
                    {
                        if (!creatorLoaded)
                        {
                            if (!ConstructorInfo.DeclaringType.IsAbstract && !ConstructorInfo.DeclaringType.IsGenericTypeDefinition)
                            {
                                this.creator = AccessorGenerator.GenerateCreator(ConstructorInfo);
                            }
                            creatorLoaded = true;
                        }
                    }
                }
                return this.creator;
            }
        }

        public override string ToString()
        {
            return $"new({(String.Join(", ", ParametersInfo.Select(x => $"{x.ParameterType.Name} {x.Name}").ToArray()))})";
        }

        private readonly object locker;
        internal ConstructorDetails(ConstructorInfo constructor, object locker)
        {
            this.locker = locker;
            this.ConstructorInfo = constructor;
            this.Name = constructor.Name;
        }
    }
}
