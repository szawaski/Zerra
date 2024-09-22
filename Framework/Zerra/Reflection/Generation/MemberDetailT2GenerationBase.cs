using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection.Generation
{
    public abstract class MemberDetailGenerationBase<T, V> : MemberDetail<T, V>
    {
        protected readonly object locker;
        private readonly Action loadMemberInfo;
        public MemberDetailGenerationBase(object locker, Action loadMemberInfo)
        {
            this.locker = locker;
            this.loadMemberInfo = loadMemberInfo;
        }

        public override sealed TypeDetail<V> TypeDetail => TypeAnalyzer<V>.GetTypeDetail();
        public override sealed TypeDetail TypeDetailBoxed => TypeDetail;

        private bool memberInfoLoaded = false;
        private MemberInfo? memberInfo = null;
        private MemberDetail<T, V>? backingFieldDetail = null;
        private MemberDetail? backingFieldDetailBoxed = null;
        public override sealed MemberInfo MemberInfo
        {
            get
            {
                if (!memberInfoLoaded)
                {
                    lock (locker)
                    {
                        if (!memberInfoLoaded)
                        {
                            loadMemberInfo();
                            memberInfoLoaded = true;
                        }
                    }
                }
                return memberInfo!;
            }
        }
        public override sealed MemberDetail<T, V>? BackingFieldDetail
        {
            get
            {
                if (!memberInfoLoaded)
                {
                    lock (locker)
                    {
                        if (!memberInfoLoaded)
                        {
                            loadMemberInfo();
                            memberInfoLoaded = true;
                            //if (IsBacked)
                            //{
                            //    var fields = Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                            //    //<{property.Name}>k__BackingField
                            //    //<{property.Name}>i__Field
                            //    var backingName = $"<{Name}>";
                            //    var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                            //    if (backingField != null)
                            //    {
                            //        backingFieldDetail = new MemberDetailRuntime<T, V>(backingField, null, locker);
                            //        backingFieldDetailBoxed = backingFieldDetail;
                            //    }
                            //}
                            //backingFieldDetailLoaded = true;
                        }
                    }
                }
                return backingFieldDetail;
            }
        }
        public override sealed MemberDetail? BackingFieldDetailBoxed
        {
            get
            {
                if (!memberInfoLoaded)
                {
                    lock (locker)
                    {
                        if (!memberInfoLoaded)
                        {
                            loadMemberInfo();
                            memberInfoLoaded = true;
                            //if (IsBacked)
                            //{
                            //    var fields = Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                            //    //<{property.Name}>k__BackingField
                            //    //<{property.Name}>i__Field
                            //    var backingName = $"<{Name}>";
                            //    var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                            //    if (backingField != null)
                            //    {
                            //        backingFieldDetail = new MemberDetailRuntime<T, V>(backingField, null, locker);
                            //        backingFieldDetailBoxed = backingFieldDetail;
                            //    }
                            //}
                            //backingFieldDetailLoaded = true;
                        }
                    }
                }
                return backingFieldDetail;
            }
        }

        internal override sealed void SetMemberInfo(MemberInfo memberInfo, MemberDetail? backingFieldDetailBoxed)
        {
            this.memberInfo = memberInfo;
            this.backingFieldDetail = (MemberDetail<T, V>?)backingFieldDetailBoxed;
            this.backingFieldDetailBoxed = backingFieldDetailBoxed;
        }
    }
}
