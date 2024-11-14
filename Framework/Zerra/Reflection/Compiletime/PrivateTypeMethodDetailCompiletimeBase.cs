using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Zerra.Reflection.Compiletime
{
    public abstract class PrivateTypeMethodDetailCompiletimeBase : MethodDetail
    {
        protected readonly object locker;
        private readonly Action loadMethodInfo;
        public PrivateTypeMethodDetailCompiletimeBase(object locker, Action loadMethodInfo)
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

        private TypeDetail? returnType = null;
        public override sealed TypeDetail ReturnTypeDetailBoxed
        {
            get
            {
                if (returnType is null)
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

        internal override sealed void SetMethodInfo(MethodInfo methodInfo)
        {
            this.methodInfo = methodInfo;
        }

        private bool callerBoxedLoaded = false;
        private Func<object?, object?[]?, object?>? callerBoxed = null;
        public override sealed Func<object?, object?[]?, object?> CallerBoxed
        {
            get
            {
                LoadCallerBoxed();
                return this.callerBoxed ?? throw new NotSupportedException($"{nameof(MethodDetail)} {Name} does not have a {nameof(CallerBoxed)}");
            }
        }
        public override sealed bool HasCallerBoxed
        {
            get
            {
                LoadCallerBoxed();
                return this.callerBoxed is not null;
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
                        if (!MethodInfo.IsGenericMethodDefinition && !MethodInfo.ReturnType.IsByRef
#if !NETSTANDARD2_0
                            && !MethodInfo.ReturnType.IsByRefLike
#endif
                        )
                        {
                            this.callerBoxed = AccessorGenerator.GenerateCaller(MethodInfo);
                        }

                        callerBoxedLoaded = true;
                    }
                }
            }
        }

        public override Delegate? CallerTyped => throw new NotSupportedException();

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
