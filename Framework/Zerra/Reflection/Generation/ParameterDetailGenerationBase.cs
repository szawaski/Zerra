using System.Reflection;
using System;

namespace Zerra.Reflection.Generation
{
    public abstract class ParameterDetailGenerationBase : ParameterDetail
    {
        protected readonly object locker;
        private readonly Action loadParameterInfo;
        public ParameterDetailGenerationBase(object locker, Action loadParameterInfo)
        {
            this.locker = locker;
            this.loadParameterInfo = loadParameterInfo;
        }

        private ParameterInfo? parameterInfo = null;
        public override sealed ParameterInfo ParameterInfo
        {
            get
            {
                if (parameterInfo is null)
                {
                    lock (locker)
                    {
                        if (parameterInfo is null)
                        {
                            loadParameterInfo();
                        }
                    }
                }
                return parameterInfo!;
            }
        }

        internal void SetParameterInfo(ParameterInfo parameterInfo)
        {
            this.parameterInfo = parameterInfo;
        }
    }
}
