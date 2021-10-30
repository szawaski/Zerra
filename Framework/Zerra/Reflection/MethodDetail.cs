// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Zerra.Reflection
{
    public class MethodDetail
    {
        public MethodInfo MethodInfo { get; private set; }
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
                        if (this.parameterInfos == null)
                            this.parameterInfos = MethodInfo.GetParameters();
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
                        if (this.attributes == null)
                            this.attributes = MethodInfo.GetCustomAttributes().ToArray();
                    }
                }
                return this.attributes;
            }
        }

        private bool callerLoaded = false;
        private Func<object, object[], object> caller = null;
        private Func<object, object[], Task<object>> callerAsync = null;
        public Func<object, object[], object> Caller
        {
            get
            {
                if (!callerLoaded)
                {
                    lock (this)
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
        public Func<object, object[], Task<object>> CallerAsync
        {
            get
            {
                if (!callerLoaded)
                {
                    lock (this)
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

        private TypeDetail returnType = null;
        public TypeDetail ReturnType
        {
            get
            {
                if (returnType == null)
                {
                    lock (this)
                    {
                        if (returnType == null)
                            returnType = TypeAnalyzer.GetTypeDetail(MethodInfo.ReturnType);
                    }
                }
                return returnType;
            }
        }

        public override string ToString()
        {
            return $"{Name}({(String.Join(", ", ParametersInfo.Select(x => $"{x.ParameterType.Name} {x.Name}").ToArray()))})";
        }

        internal MethodDetail(MethodInfo method)
        {
            this.MethodInfo = method;
            this.Name = method.Name;
        }

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
                    var returnTypeInfo = ReturnType;

                    if (returnTypeInfo.IsTask)
                    {
                        var result = Caller(source, arguments);
                        var task = result as Task;
                        await task;
                        if (returnTypeInfo.Type.IsGenericType)
                            return returnTypeInfo.TaskResultGetter(result);
                        else
                            return default;
                    }
                    else
                    {
                        return Caller(source, arguments);
                    }
                };
            }
        }
    }
}
