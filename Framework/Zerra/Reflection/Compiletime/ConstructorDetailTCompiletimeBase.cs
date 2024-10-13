using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.Reflection.Compiletime
{
    public abstract class ConstructorDetailCompiletimeBase<T> : ConstructorDetail<T>
    {
        protected readonly object locker;
        private readonly Action loadConstructorInfo;
        public ConstructorDetailCompiletimeBase(object locker, Action loadConstructorInfo)
        {
            this.locker = locker;
            this.loadConstructorInfo = loadConstructorInfo;
        }

        public override sealed bool IsGenerated => true;

        private ConstructorInfo? constructorInfo = null;
        public override sealed ConstructorInfo ConstructorInfo
        {
            get
            {
                if (constructorInfo is null)
                {
                    lock (locker)
                    {
                        if (constructorInfo is null)
                        {
                            loadConstructorInfo();
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

        public override sealed Delegate? CreatorTyped => Creator;
        public override sealed Delegate? CreatorWithArgsTyped => CreatorWithArgs;

        internal override sealed void SetConstructorInfo(ConstructorInfo constructorInfo)
        {
            this.constructorInfo = constructorInfo;
        }

        protected void LoadParameterInfo()
        {
            var parameters = ConstructorInfo.GetParameters();
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
