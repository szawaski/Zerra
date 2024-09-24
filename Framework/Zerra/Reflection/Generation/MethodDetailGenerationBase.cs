﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Zerra.Reflection.Generation
{
    public abstract class MethodDetailGenerationBase<T> : MethodDetail<T>
    {
        protected readonly object locker;
        private readonly Action loadMethodInfo;
        public MethodDetailGenerationBase(object locker, Action loadMethodInfo)
        {
            this.locker = locker;
            this.loadMethodInfo = loadMethodInfo;
        }

        private MethodInfo? constructorInfo = null;
        public override sealed MethodInfo MethodInfo
        {
            get
            {
                if (constructorInfo is null)
                {
                    lock (locker)
                    {
                        if (constructorInfo is null)
                        {
                            loadMethodInfo();
                        }
                    }
                }
                return constructorInfo!;
            }
        }

        private TypeDetail? returnType = null;
        public override sealed TypeDetail ReturnTypeDetail
        {
            get
            {
                if (returnType == null)
                {
                    lock (locker)
                    {
                        returnType ??= TypeAnalyzer.GetTypeDetail(ReturnType);
                    }
                }
                return returnType;
            }
        }

        protected abstract Func<Attribute[]> CreateAttributes { get; }
        private Attribute[]? attributes = null;
        public override sealed IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (attributes is null)
                {
                    lock (locker)
                    {
                        attributes ??= CreateAttributes();
                    }
                }
                return attributes;
            }
        }

        protected abstract Func<ParameterDetail[]> CreateParameterDetails { get; }
        private ParameterDetail[]? parameters = null;
        public override sealed IReadOnlyList<ParameterDetail> ParameterDetails
        {
            get
            {
                if (parameters is null)
                {
                    lock (locker)
                    {
                        parameters ??= CreateParameterDetails();
                    }
                }
                return parameters;
            }
        }

        private bool callerBoxedLoaded = false;
        private Func<object?, object?[]?, Task<object?>>? callerBoxedAsync = null;
        public override sealed Func<object?, object?[]?, Task<object?>> CallerBoxedAsync
        {
            get
            {
                LoadCallerBoxedAsync();
                return this.callerBoxedAsync ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(CallerAsync)}");
            }
        }
        public override sealed bool HasCallerBoxedAsync
        {
            get
            {
                LoadCallerBoxedAsync();
                return this.callerBoxedAsync != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCallerBoxedAsync()
        {
            if (!callerBoxedLoaded)
            {
                lock (locker)
                {
                    if (!callerBoxedLoaded)
                    {
                        if (HasCallerBoxed)
                        {
                            this.callerBoxedAsync = async (source, arguments) =>
                            {
                                var caller = CallerBoxed;
                                if (caller == null)
                                    return default;
                                var returnTypeInfo = ReturnTypeDetail;

                                if (returnTypeInfo.IsTask)
                                {
                                    var result = caller(source, arguments)!;
                                    var task = (Task)result;
                                    await task;
                                    if (returnTypeInfo.Type.IsGenericType)
                                        return returnTypeInfo.TaskResultGetter(result);
                                    else
                                        return default;
                                }
                                else
                                {
                                    return caller(source, arguments);
                                }
                            };
                        }

                        callerBoxedLoaded = true;
                    }
                }
            }
        }

        private bool callerLoaded = false;
        private Func<T?, object?[]?, Task<object?>>? callerAsync = null;
        public override sealed Func<T?, object?[]?, Task<object?>> CallerAsync
        {
            get
            {
                LoadCallerAsync();
                return this.callerAsync ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(CallerAsync)}");
            }
        }
        public override sealed bool HasCallerAsync
        {
            get
            {
                LoadCallerAsync();
                return this.callerAsync != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCallerAsync()
        {
            if (!callerLoaded)
            {
                lock (locker)
                {
                    if (!callerLoaded)
                    {
                        if (HasCaller)
                        {
                            this.callerAsync = async (source, arguments) =>
                            {
                                var caller = this.Caller;
                                if (caller == null)
                                    return default;
                                var returnTypeInfo = ReturnTypeDetail;

                                if (returnTypeInfo.IsTask)
                                {
                                    var result = caller(source, arguments)!;
                                    var task = (Task)result;
                                    await task;
                                    if (returnTypeInfo.Type.IsGenericType)
                                        return returnTypeInfo.TaskResultGetter(result);
                                    else
                                        return default;
                                }
                                else
                                {
                                    return caller(source, arguments);
                                }
                            };
                        }

                        callerLoaded = true;
                    }
                }
            }
        }

        public override sealed Delegate? CallerTyped => Caller;

        internal void SetMethodInfo(MethodInfo constructorInfo)
        {
            this.constructorInfo = constructorInfo;
        }

        protected void LoadParameterInfo()
        {
            var parameters = MethodInfo.GetParameters();
            foreach (var parameterDetail in ParameterDetails)
            {
                var parameter = parameters.FirstOrDefault(x => x.Name == parameterDetail.Name);
                if (parameter == null)
                    throw new InvalidOperationException($"Parameter not found for {parameterDetail.Name}");

                var parameterBase = (ParameterDetailGenerationBase)parameterDetail;
                parameterBase.SetParameterInfo(parameter);
            }
        }
    }
}
