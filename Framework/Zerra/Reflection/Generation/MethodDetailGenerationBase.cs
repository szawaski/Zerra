using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        public override MethodInfo MethodInfo
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
