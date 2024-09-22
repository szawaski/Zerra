//Zerra Generated File

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Reflection.Generation;
using ZerraDemo.Domain.Ledger1.Models;

namespace ZerraDemo.Domain.Ledger1.Models.SourceGeneration
{
    public sealed class Balance1ModelTypeDetail : TypeDetailTGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model>
    {               
        public override bool HasIEnumerable => false;
        public override bool HasIEnumerableGeneric => false;
        public override bool HasICollection => false;
        public override bool HasICollectionGeneric => false;
        public override bool HasIReadOnlyCollectionGeneric => false;
        public override bool HasIList => false;
        public override bool HasIListGeneric => false;
        public override bool HasIReadOnlyListGeneric => false;
        public override bool HasISetGeneric => false;
        public override bool HasIReadOnlySetGeneric => false;
        public override bool HasIDictionary => false;
        public override bool HasIDictionaryGeneric => false;
        public override bool HasIReadOnlyDictionaryGeneric => false;

        public override bool IsIEnumerable => false;
        public override bool IsIEnumerableGeneric => false;
        public override bool IsICollection => false;
        public override bool IsICollectionGeneric => false;
        public override bool IsIReadOnlyCollectionGeneric => false;
        public override bool IsIList => false;
        public override bool IsIListGeneric => false;
        public override bool IsIReadOnlyListGeneric => false;
        public override bool IsISetGeneric => false;
        public override bool IsIReadOnlySetGeneric => false;
        public override bool IsIDictionary => false;
        public override bool IsIDictionaryGeneric => false;
        public override bool IsIReadOnlyDictionaryGeneric => false;

        public override Func<ZerraDemo.Domain.Ledger1.Models.Balance1Model> Creator => () => new ZerraDemo.Domain.Ledger1.Models.Balance1Model();
        public override bool HasCreator => true;

        public override bool IsNullable => false;

        public override CoreType? CoreType => null;
        public override SpecialType? SpecialType => null;
        public override CoreEnumType? EnumUnderlyingType => null;

        private readonly Type? innerType = null;
        public override Type InnerType => innerType ?? throw new NotSupportedException();

        public override bool IsTask => false;

        public override IReadOnlyList<Type> BaseTypes => [typeof(System.Object)];

        public override IReadOnlyList<Type> Interfaces => [];

        public override IReadOnlyList<Attribute> Attributes => [];

        public override IReadOnlyList<Type> InnerTypes => [];
        public override IReadOnlyList<TypeDetail> InnerTypeDetails => [];

        public override TypeDetail InnerTypeDetail => throw new NotSupportedException();

        public override Type IEnumerableGenericInnerType => throw new NotSupportedException();
        public override TypeDetail IEnumerableGenericInnerTypeDetail => throw new NotSupportedException();

        public override Type DictionaryInnerType => throw new NotSupportedException();
        public override TypeDetail DictionaryInnerTypeDetail => throw new NotSupportedException();

        public override Func<object, object?> TaskResultGetter => throw new NotSupportedException();
        public override bool HasTaskResultGetter => false;

        public override Func<object> CreatorBoxed => () => new ZerraDemo.Domain.Ledger1.Models.Balance1Model();
        public override bool HasCreatorBoxed => true;

        protected override Func<MethodDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model>[]> CreateMethodDetails => () => [];

        protected override Func<ConstructorDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model>[]> CreateConstructorDetails => () => [];

        protected override Func<MemberDetail[]> CreateMemberDetails => () => [new AccountIDMemberDetail(locker, LoadMemberInfo), new BalanceMemberDetail(locker, LoadMemberInfo), new LastTransactionDateMemberDetail(locker, LoadMemberInfo), new LastTransactionAmountMemberDetail(locker, LoadMemberInfo)];


        public sealed class AccountIDMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.Guid>
        {
            public AccountIDMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "AccountID";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.Guid> Getter => (x) => x.AccountID;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.Guid> Setter => (x, value) => x.AccountID = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Balance1Model)x).AccountID;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Balance1Model)x).AccountID = (System.Guid)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.Guid>?> CreateBackingFieldDetail => () => new _AccountID_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class BalanceMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal>
        {
            public BalanceMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Balance";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal> Getter => (x) => x.Balance;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal> Setter => (x, value) => x.Balance = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Balance1Model)x).Balance;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Balance1Model)x).Balance = (decimal)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal>?> CreateBackingFieldDetail => () => new _Balance_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class LastTransactionDateMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.DateTime?>
        {
            public LastTransactionDateMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "LastTransactionDate";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.DateTime?> Getter => (x) => x.LastTransactionDate;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.DateTime?> Setter => (x, value) => x.LastTransactionDate = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Balance1Model)x).LastTransactionDate;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Balance1Model)x).LastTransactionDate = (System.DateTime?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.DateTime?>?> CreateBackingFieldDetail => () => new _LastTransactionDate_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class LastTransactionAmountMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal?>
        {
            public LastTransactionAmountMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "LastTransactionAmount";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal?> Getter => (x) => x.LastTransactionAmount;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal?> Setter => (x, value) => x.LastTransactionAmount = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Balance1Model)x).LastTransactionAmount;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Balance1Model)x).LastTransactionAmount = (decimal?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal?>?> CreateBackingFieldDetail => () => new _LastTransactionAmount_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class _AccountID_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.Guid>
        {
            public _AccountID_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<AccountID>k__BackingField";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.Guid>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Balance_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal>
        {
            public _Balance_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Balance>k__BackingField";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _LastTransactionDate_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.DateTime?>
        {
            public _LastTransactionDate_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<LastTransactionDate>k__BackingField";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model, System.DateTime?>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _LastTransactionAmount_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal?>
        {
            public _LastTransactionAmount_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<LastTransactionAmount>k__BackingField";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Balance1Model, decimal?>?> CreateBackingFieldDetail => () => null;
        }
    }
}