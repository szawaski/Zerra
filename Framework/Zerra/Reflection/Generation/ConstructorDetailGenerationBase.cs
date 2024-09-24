using System.Reflection;
using System;
using System.Collections.Generic;

namespace Zerra.Reflection.Generation
{
    public abstract class ConstructorDetailGenerationBase<T> : ConstructorDetail<T>
    {
        protected readonly object locker;
        private readonly Action loadConstructorInfo;
        public ConstructorDetailGenerationBase(object locker, Action loadConstructorInfo)
        {
            this.locker = locker;
            this.loadConstructorInfo = loadConstructorInfo;
        }

        private ConstructorInfo? constructorInfo = null;
        public override ConstructorInfo ConstructorInfo
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

        protected abstract Func<ParameterDetail[]> CreateParameters { get; }
        private ParameterDetail[]? parameters = null;
        public override sealed IReadOnlyList<ParameterDetail> Parameters
        {
            get
            {
                if (parameters is null)
                {
                    lock (locker)
                    {
                        parameters ??= CreateParameters();
                    }
                }
                return parameters;
            }
        }

        internal void SetConstructorInfo(ConstructorInfo constructorInfo)
        {
            this.constructorInfo = constructorInfo;
        }
    }
}
