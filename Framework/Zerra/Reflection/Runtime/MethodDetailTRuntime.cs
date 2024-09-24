// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Zerra.Reflection.Runtime
{
    internal sealed class MethodDetailRuntime<T> : MethodDetail<T>
    {
        public override MethodInfo MethodInfo { get; }
        public override string Name => MethodInfo.Name;

        private ParameterDetail[]? parameterInfos = null;
        public override IReadOnlyList<ParameterDetail> ParameterDetails
        {
            get
            {
                if (this.parameterInfos == null)
                {
                    lock (locker)
                    {
                        var parameters = MethodInfo.GetParameters();
                        this.parameterInfos ??= parameters.Select(x => new ParameterDetailRuntime(x, locker)).ToArray();
                    }
                }
                return this.parameterInfos;
            }
        }

        private Attribute[]? attributes = null;
        public override IReadOnlyList<Attribute> Attributes
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

        private bool callerBoxedLoaded = false;
        private Func<object?, object?[]?, object?>? callerBoxed = null;
        private Func<object?, object?[]?, Task<object?>>? callerBoxedAsync = null;
        public override Func<object?, object?[]?, object?> CallerBoxed
        {
            get
            {
                LoadCallerBoxed();
                return this.callerBoxed ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(Caller)}");
            }
        }
        public override bool HasCallerBoxed
        {
            get
            {
                LoadCallerBoxed();
                return this.callerBoxed != null;
            }
        }
        public override Func<object?, object?[]?, Task<object?>> CallerBoxedAsync
        {
            get
            {
                LoadCallerBoxed();
                return this.callerBoxedAsync ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(CallerAsync)}");
            }
        }
        public override bool HasCallerBoxedAsync
        {
            get
            {
                LoadCallerBoxed();
                return this.callerBoxedAsync != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCallerBoxed()
        {
            if (!callerBoxedLoaded)
            {
                lock (locker)
                {
                    if (!callerBoxedLoaded)
                    {
#if NETSTANDARD2_0
                        if (!MethodInfo.IsGenericMethodDefinition && !MethodInfo.ReturnType.IsByRef)
#else
                        if (!MethodInfo.IsGenericMethodDefinition && !MethodInfo.ReturnType.IsByRef && !MethodInfo.ReturnType.IsByRefLike)
#endif
                        {
                            this.callerBoxed = AccessorGenerator.GenerateCaller(MethodInfo);
                            this.callerBoxedAsync = async (source, arguments) =>
                            {
                                var caller = this.callerBoxed;
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

        public override Type ReturnType => MethodInfo.ReturnType;

        private TypeDetail? returnType = null;
        public override TypeDetail ReturnTypeDetail
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

        private bool callerLoaded = false;
        private Func<T?, object?[]?, object?>? caller = null;
        private Func<T?, object?[]?, Task<object?>>? callerAsync = null;
        public override Func<T?, object?[]?, object?> Caller
        {
            get
            {
                LoadCaller();
                return this.caller ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(Caller)}");
            }
        }
        public override bool HasCaller
        {
            get
            {
                LoadCaller();
                return this.caller != null;
            }
        }
        public override Func<T?, object?[]?, Task<object?>> CallerAsync
        {
            get
            {
                LoadCaller();
                return this.callerAsync ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(CallerAsync)}");
            }
        }
        public override bool HasCallerAsync
        {
            get
            {
                LoadCaller();
                return this.callerAsync != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCaller()
        {
            if (!callerLoaded)
            {
                lock (locker)
                {
                    if (!callerLoaded)
                    {
#if NETSTANDARD2_0
                        if (!MethodInfo.IsGenericMethodDefinition && !MethodInfo.ReturnType.IsByRef)
#else
                        if (!MethodInfo.IsGenericMethodDefinition && !MethodInfo.ReturnType.IsByRef && !MethodInfo.ReturnType.IsByRefLike)
#endif
                        {
                            this.caller = AccessorGenerator.GenerateCaller<T>(MethodInfo);
                            this.callerAsync = async (source, arguments) =>
                            {
                                var caller = this.caller;
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

        public override Delegate? CallerTyped => Caller;

        private readonly object locker;
        internal MethodDetailRuntime(MethodInfo method, object locker)
        {
            this.locker = locker;
            this.MethodInfo = method;
        }

        private static readonly Type typeDetailT = typeof(MethodDetailRuntime<>);
        internal static MethodDetail New(Type type, MethodInfo method, object locker)
        {
            var typeDetailGeneric = typeDetailT.MakeGenericType(type);
            var obj = typeDetailGeneric.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object[] { method, locker });
            return (MethodDetail)obj;
        }
    }
}
