using System.Reflection;
using System;

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

        internal void SetConstructorInfo(ConstructorInfo constructorInfo)
        {
            this.constructorInfo = constructorInfo;
        }
    }
}
