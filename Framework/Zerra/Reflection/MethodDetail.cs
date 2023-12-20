// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Zerra.Reflection
{
    public sealed class MethodDetail
    {
        public MethodInfo MethodInfo { get; private set; }
        public string Name => MethodInfo.Name;

        private ParameterInfo[]? parameterInfos = null;
        public IReadOnlyList<ParameterInfo> ParametersInfo
        {
            get
            {
                if (this.parameterInfos == null)
                {
                    lock (locker)
                    {
                        this.parameterInfos ??= MethodInfo.GetParameters();
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
                        this.attributes ??= MethodInfo.GetCustomAttributes().ToArray();
                    }
                }
                return this.attributes;
            }
        }

        private bool callerLoaded = false;
        private Func<object?, object[]?, object?>? caller = null;
        private Func<object?, object[]?, Task<object?>>? callerAsync = null;
        public Func<object?, object[]?, object?>? Caller
        {
            get
            {
                if (!callerLoaded)
                {
                    lock (locker)
                    {
                        if (!callerLoaded)
                        {
                            LoadCaller();
                            callerLoaded = true;
                        }
                    }
                }
                return this.caller;
            }
        }
        public Func<object?, object[]?, Task<object?>>? CallerAsync
        {
            get
            {
                if (!callerLoaded)
                {
                    lock (locker)
                    {
                        if (!callerLoaded)
                        {
                            LoadCaller();
                            callerLoaded = true;
                        }
                    }
                }
                return this.callerAsync;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCaller()
        {
#if NETSTANDARD2_0
            if (!MethodInfo.IsGenericMethodDefinition && !MethodInfo.ReturnType.IsByRef)
#else
            if (!MethodInfo.IsGenericMethodDefinition && !MethodInfo.ReturnType.IsByRef && !MethodInfo.ReturnType.IsByRefLike)
#endif
            {
                this.caller = AccessorGenerator.GenerateCaller(MethodInfo);
                this.callerAsync = async (source, arguments) =>
                {
                    var caller = this.caller;
                    if (caller == null)
                        return default;
                    var returnTypeInfo = ReturnType;

                    if (returnTypeInfo.IsTask)
                    {
                        var result = caller(source, arguments);
                        var task = result as Task;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        await task;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        if (returnTypeInfo.Type.IsGenericType)
#pragma warning disable CS8604 // Possible null reference argument.
                            return returnTypeInfo.TaskResultGetter(result);
#pragma warning restore CS8604 // Possible null reference argument.
                        else
                            return default;
                    }
                    else
                    {
                        return caller(source, arguments);
                    }
                };
            }
        }

        private TypeDetail? returnType = null;
        public TypeDetail ReturnType
        {
            get
            {
                if (returnType == null)
                {
                    lock (locker)
                    {
                        returnType ??= TypeAnalyzer.GetTypeDetail(MethodInfo.ReturnType);
                    }
                }
                return returnType;
            }
        }

        public override string ToString()
        {
            return $"{Name}({(String.Join(", ", ParametersInfo.Select(x => $"{x.ParameterType.Name} {x.Name}").ToArray()))})";
        }

        private readonly object locker;
        internal MethodDetail(MethodInfo method, object locker)
        {
            this.locker = locker;
            this.MethodInfo = method;
        }
    }
}
