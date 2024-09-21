using System;
using System.Collections.Generic;

namespace Zerra.Reflection.Generation
{
    public abstract class TypeDetailTGenerationBase<T> : TypeDetail<T>
    {
        public TypeDetailTGenerationBase() : base(typeof(T)) { }

        protected abstract Func<MethodDetail<T>[]> CreateMethodDetails { get; }
        private MethodDetail<T>[]? methodDetails = null;
        public override sealed IReadOnlyList<MethodDetail<T>> MethodDetails
        {
            get
            {
                if (methodDetails is null)
                {
                    lock (locker)
                    {
                        methodDetails ??= CreateMethodDetails();
                    }
                }
                return methodDetails;
            }
        }
        public override sealed IReadOnlyList<MethodDetail> MethodDetailsBoxed
        {
            get
            {
                if (methodDetails is null)
                {
                    lock (locker)
                    {
                        methodDetails ??= CreateMethodDetails();
                    }
                }
                return methodDetails;
            }
        }

        protected abstract Func<ConstructorDetail<T>[]> CreateConstructorDetails { get; }
        private ConstructorDetail<T>[]? constructorDetails = null;
        public override sealed IReadOnlyList<ConstructorDetail<T>> ConstructorDetails
        {
            get
            {
                if (constructorDetails is null)
                {
                    lock (locker)
                    {
                        constructorDetails ??= CreateConstructorDetails();
                    }
                }
                return constructorDetails;
            }
        }
        public override sealed IReadOnlyList<ConstructorDetail> ConstructorDetailsBoxed
        {
            get
            {
                if (constructorDetails is null)
                {
                    lock (locker)
                    {
                        constructorDetails ??= CreateConstructorDetails();
                    }
                }
                return constructorDetails;
            }
        }

        protected abstract Func<MemberDetail[]> CreateMemberDetails { get; }
        private MemberDetail[]? memberDetails = null;
        public override sealed IReadOnlyList<MemberDetail> MemberDetails
        {
            get
            {
                if (memberDetails is null)
                {
                    lock (locker)
                    {
                        memberDetails ??= CreateMemberDetails();
                    }
                }
                return memberDetails;
            }
        }
    }
}
