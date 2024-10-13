using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Zerra.Reflection.Compiletime
{
    public abstract class MethodDetailCompiletimeBase<T> : MethodDetail<T>
    {
        protected readonly object locker;
        private readonly Action loadMethodInfo;
        public MethodDetailCompiletimeBase(object locker, Action loadMethodInfo)
        {
            this.locker = locker;
            this.loadMethodInfo = loadMethodInfo;
        }

        public override sealed bool IsGenerated => true;

        private MethodInfo? methodInfo = null;
        public override sealed MethodInfo MethodInfo
        {
            get
            {
                if (methodInfo is null)
                {
                    lock (locker)
                    {
                        if (methodInfo is null)
                        {
                            loadMethodInfo();
                        }
                    }
                }
                return methodInfo!;
            }
        }

        private TypeDetail? returnTypeDetailBoxed = null;
        public override sealed TypeDetail ReturnTypeDetailBoxed
        {
            get
            {
                if (returnTypeDetailBoxed is null)
                {
                    lock (locker)
                    {
                        returnTypeDetailBoxed ??= TypeAnalyzer.GetTypeDetail(ReturnType);
                    }
                }
                return returnTypeDetailBoxed;
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
                return this.callerBoxedAsync is not null;
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
                                if (caller is null)
                                    return default;
                                var returnTypeInfo = ReturnTypeDetailBoxed;

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
                return this.callerAsync is not null;
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
                                if (caller is null)
                                    return default;
                                var returnTypeInfo = ReturnTypeDetailBoxed;

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

        internal override sealed void SetMethodInfo(MethodInfo methodInfo)
        {
            this.methodInfo = methodInfo;
        }

        protected void LoadParameterInfo()
        {
            var parameters = MethodInfo.GetParameters();
            foreach (var parameterDetail in ParameterDetails)
            {
                var parameter = parameters.FirstOrDefault(x => x.Name == parameterDetail.Name);
                if (parameter is null)
                    throw new InvalidOperationException($"Parameter not found for {parameterDetail.Name}");

                var parameterBase = (ParameterDetailCompiletimeBase)parameterDetail;
                parameterBase.SetParameterInfo(parameter);
            }
        }
    }
}
