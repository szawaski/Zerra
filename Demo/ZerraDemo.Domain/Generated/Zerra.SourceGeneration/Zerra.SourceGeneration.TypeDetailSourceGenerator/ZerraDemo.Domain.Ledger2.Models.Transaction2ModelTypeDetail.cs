//Zerra Generated File

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Reflection.Generation;
using ZerraDemo.Domain.Ledger2.Models;

namespace ZerraDemo.Domain.Ledger2.Models.SourceGeneration
{
    public sealed class Transaction2ModelTypeDetail : TypeDetailTGenerationBase<ZerraDemo.Domain.Ledger2.Models.Transaction2Model>
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

        public override Func<ZerraDemo.Domain.Ledger2.Models.Transaction2Model> Creator => () => new ZerraDemo.Domain.Ledger2.Models.Transaction2Model();
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

        public override Func<object> CreatorBoxed => () => new ZerraDemo.Domain.Ledger2.Models.Transaction2Model();
        public override bool HasCreatorBoxed => true;

        protected override Func<MethodDetail<ZerraDemo.Domain.Ledger2.Models.Transaction2Model>[]> CreateMethodDetails => () => [];

        protected override Func<ConstructorDetail<ZerraDemo.Domain.Ledger2.Models.Transaction2Model>[]> CreateConstructorDetails => () => [];

        protected override Func<MemberDetail[]> CreateMemberDetails => () => [new AccountIDMemberDetail(locker, LoadMemberInfo), new DateMemberDetail(locker, LoadMemberInfo), new AmountMemberDetail(locker, LoadMemberInfo), new DescriptionMemberDetail(locker, LoadMemberInfo), new BalanceMemberDetail(locker, LoadMemberInfo), new EventMemberDetail(locker, LoadMemberInfo)];


        public sealed class AccountIDMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, System.Guid>
        {
            public AccountIDMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "AccountID";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, System.Guid> Getter => (x) => x.AccountID;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, System.Guid> Setter => (x, value) => x.AccountID = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).AccountID;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).AccountID = (System.Guid)value!;
            public override bool HasSetterBoxed => true;
        }
        public sealed class DateMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, System.DateTime>
        {
            public DateMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Date";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, System.DateTime> Getter => (x) => x.Date;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, System.DateTime> Setter => (x, value) => x.Date = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Date;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Date = (System.DateTime)value!;
            public override bool HasSetterBoxed => true;
        }
        public sealed class AmountMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, decimal>
        {
            public AmountMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Amount";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, decimal> Getter => (x) => x.Amount;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, decimal> Setter => (x, value) => x.Amount = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Amount;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Amount = (decimal)value!;
            public override bool HasSetterBoxed => true;
        }
        public sealed class DescriptionMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, string?>
        {
            public DescriptionMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Description";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, string?> Getter => (x) => x.Description;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, string?> Setter => (x, value) => x.Description = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Description;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Description = (string?)value!;
            public override bool HasSetterBoxed => true;
        }
        public sealed class BalanceMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, decimal>
        {
            public BalanceMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Balance";

            private readonly Type type = typeof(decimal);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, decimal> Getter => (x) => x.Balance;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, decimal> Setter => (x, value) => x.Balance = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Balance;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Balance = (decimal)value!;
            public override bool HasSetterBoxed => true;
        }
        public sealed class EventMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, string?>
        {
            public EventMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Event";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, string?> Getter => (x) => x.Event;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Ledger2.Models.Transaction2Model, string?> Setter => (x, value) => x.Event = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Event;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Ledger2.Models.Transaction2Model)x).Event = (string?)value!;
            public override bool HasSetterBoxed => true;
        }
    }
}