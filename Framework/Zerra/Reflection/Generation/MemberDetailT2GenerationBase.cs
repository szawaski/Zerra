using System;
using System.Linq;
using System.Reflection;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection.Generation
{
    public abstract class MemberDetailGenerationBase<T, V> : MemberDetail<T, V>
    {
        protected readonly object locker;
        public MemberDetailGenerationBase(object locker) => this.locker = locker;

        public override sealed TypeDetail<V> TypeDetail => TypeAnalyzer<V>.GetTypeDetail();
        public override sealed TypeDetail TypeDetailBoxed => TypeDetail;

        private MemberInfo? memberInfo = null;
        public override sealed MemberInfo MemberInfo
        {
            get
            {
                if (memberInfo is null)
                {
                    lock (locker)
                    {
                        if (memberInfo is null)
                        {
                            memberInfo ??= typeof(T).GetProperty(nameof(Name))!;
                        }
                    }
                }
                return memberInfo;
            }
        }

        private bool backingFieldDetailLoaded = false;
        private MemberDetail<T, V>? backingFieldDetail = null;
        private MemberDetail? backingFieldDetailBoxed = null;
        public override sealed MemberDetail<T, V>? BackingFieldDetail
        {
            get
            {
                if (!backingFieldDetailLoaded)
                {
                    lock (locker)
                    {
                        if (!backingFieldDetailLoaded)
                        {
                            if (IsBacked)
                            {
                                var fields = Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                                //<{property.Name}>k__BackingField
                                //<{property.Name}>i__Field
                                var backingName = $"<{Name}>";
                                var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                                if (backingField != null)
                                {
                                    backingFieldDetail = new MemberDetailRuntime<T, V>(backingField, null, locker);
                                    backingFieldDetailBoxed = backingFieldDetail;
                                }
                            }
                            backingFieldDetailLoaded = true;
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
                if (!backingFieldDetailLoaded)
                {
                    lock (locker)
                    {
                        if (!backingFieldDetailLoaded)
                        {
                            if (IsBacked)
                            {
                                var fields = Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                                //<{property.Name}>k__BackingField
                                //<{property.Name}>i__Field
                                var backingName = $"<{Name}>";
                                var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                                if (backingField != null)
                                {
                                    backingFieldDetail = new MemberDetailRuntime<T, V>(backingField, null, locker);
                                    backingFieldDetailBoxed = backingFieldDetail;
                                }
                            }
                            backingFieldDetailLoaded = true;
                        }
                    }
                }
                return backingFieldDetail;
            }
        }
    }
}
