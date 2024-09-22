//Zerra Generated File

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Reflection.Generation;
using ZerraDemo.Domain.Ledger1.Models;

namespace ZerraDemo.Domain.Ledger1.Models.SourceGeneration
{
    public sealed class Transaction1ModelTypeDetail : TypeDetailTGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model>
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

        public override Func<ZerraDemo.Domain.Ledger1.Models.Transaction1Model> Creator => () => new ZerraDemo.Domain.Ledger1.Models.Transaction1Model();
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

        public override Func<object> CreatorBoxed => () => new ZerraDemo.Domain.Ledger1.Models.Transaction1Model();
        public override bool HasCreatorBoxed => true;

        protected override Func<MethodDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model>[]> CreateMethodDetails => () => [];

        protected override Func<ConstructorDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model>[]> CreateConstructorDetails => () => [];

        protected override Func<MemberDetail[]> CreateMemberDetails => () => [new AccountIDMemberDetail(locker, LoadMemberInfo), new DateMemberDetail(locker, LoadMemberInfo), new AmountMemberDetail(locker, LoadMemberInfo), new DescriptionMemberDetail(locker, LoadMemberInfo), new BalanceMemberDetail(locker, LoadMemberInfo), new EventMemberDetail(locker, LoadMemberInfo)];


        public sealed class AccountIDMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.Guid>
        {
            public AccountIDMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "AccountID";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.Guid> Getter => (x) => x.AccountID;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.Guid> Setter => (x, value) => x.AccountID = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).AccountID;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).AccountID = (System.Guid)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.Guid>?> CreateBackingFieldDetail => () => new _AccountID_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class DateMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.DateTime>
        {
            public DateMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Date";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.DateTime> Getter => (x) => x.Date;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.DateTime> Setter => (x, value) => x.Date = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Date;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Date = (System.DateTime)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.DateTime>?> CreateBackingFieldDetail => () => new _Date_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class AmountMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal>
        {
            public AmountMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Amount";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal> Getter => (x) => x.Amount;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal> Setter => (x, value) => x.Amount = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Amount;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Amount = (decimal)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal>?> CreateBackingFieldDetail => () => new _Amount_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class DescriptionMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?>
        {
            public DescriptionMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Description";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?> Getter => (x) => x.Description;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?> Setter => (x, value) => x.Description = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Description;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Description = (string?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?>?> CreateBackingFieldDetail => () => new _Description_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class BalanceMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal>
        {
            public BalanceMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Balance";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal> Getter => (x) => x.Balance;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal> Setter => (x, value) => x.Balance = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Balance;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Balance = (decimal)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal>?> CreateBackingFieldDetail => () => new _Balance_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class EventMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?>
        {
            public EventMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Event";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?> Getter => (x) => x.Event;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?> Setter => (x, value) => x.Event = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Event;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger1.Models.Transaction1Model)x).Event = (string?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?>?> CreateBackingFieldDetail => () => new _Event_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class _AccountID_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.Guid>
        {
            public _AccountID_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<AccountID>k__BackingField";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.Guid>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Date_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.DateTime>
        {
            public _Date_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Date>k__BackingField";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, System.DateTime>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Amount_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal>
        {
            public _Amount_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Amount>k__BackingField";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Description_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?>
        {
            public _Description_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Description>k__BackingField";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Balance_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal>
        {
            public _Balance_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Balance>k__BackingField";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, decimal>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Event_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?>
        {
            public _Event_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Event>k__BackingField";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Ledger1.Models.Transaction1Model, string?>?> CreateBackingFieldDetail => () => null;
        }
    }
}