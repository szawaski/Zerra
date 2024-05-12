// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Zerra.Reflection
{
    public sealed class MethodDetail<T> : MethodDetail
    {
        private bool callerLoaded = false;
        private Func<T?, object?[]?, object?>? caller = null;
        private Func<T?, object?[]?, Task<object?>>? callerAsync = null;
        public new Func<T?, object?[]?, object?> Caller
        {
            get
            {
                LoadCaller();
                return this.caller ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(Caller)}");
            }
        }
        public new bool HasCaller
        {
            get
            {
                LoadCaller();
                return this.caller != null;
            }
        }
        public new Func<T?, object?[]?, Task<object?>> CallerAsync
        {
            get
            {
                LoadCaller();
                return this.callerAsync ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(CallerAsync)}");
            }
        }
        public new bool HasCallerAsync
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
                                var returnTypeInfo = ReturnType;

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

        internal MethodDetail(MethodInfo method, object locker) : base(method, locker) { }
    }
}
