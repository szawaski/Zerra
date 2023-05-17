// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zerra.Reflection
{
    public class ConstructorDetails
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
                    lock (this)
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
                    lock (this)
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
                    lock (this)
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

        internal ConstructorDetails(ConstructorInfo constructor)
        {
            this.ConstructorInfo = constructor;
            this.Name = constructor.Name;
        }
    }
}
