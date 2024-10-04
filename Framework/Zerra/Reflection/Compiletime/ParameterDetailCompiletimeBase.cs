using System.Reflection;
using System;

namespace Zerra.Reflection.Compiletime
{
    public abstract class ParameterDetailCompiletimeBase : ParameterDetail
    {
        protected readonly object locker;
        private readonly Action loadParameterInfo;
        public ParameterDetailCompiletimeBase(object locker, Action loadParameterInfo)
        {
            this.locker = locker;
            this.loadParameterInfo = loadParameterInfo;
        }

        public override sealed bool IsGenerated => true;

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
