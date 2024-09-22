using System;
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

        internal void SetMethodInfo(MethodInfo constructorInfo)
        {
            this.constructorInfo = constructorInfo;
        }
    }
}
