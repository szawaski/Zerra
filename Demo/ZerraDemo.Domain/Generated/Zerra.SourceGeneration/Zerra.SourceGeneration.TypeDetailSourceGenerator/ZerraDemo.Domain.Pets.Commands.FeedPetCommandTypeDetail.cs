//Zerra Generated File

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Reflection.Generation;
using ZerraDemo.Domain.Pets.Commands;

namespace ZerraDemo.Domain.Pets.Commands.SourceGeneration
{
    public sealed class FeedPetCommandTypeDetail : TypeDetailTGenerationBase<ZerraDemo.Domain.Pets.Commands.FeedPetCommand>
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

        public override Func<ZerraDemo.Domain.Pets.Commands.FeedPetCommand> Creator => () => new ZerraDemo.Domain.Pets.Commands.FeedPetCommand();
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

        public override Func<object> CreatorBoxed => () => new ZerraDemo.Domain.Pets.Commands.FeedPetCommand();
        public override bool HasCreatorBoxed => true;

        protected override Func<MethodDetail<ZerraDemo.Domain.Pets.Commands.FeedPetCommand>[]> CreateMethodDetails => () => [];

        protected override Func<ConstructorDetail<ZerraDemo.Domain.Pets.Commands.FeedPetCommand>[]> CreateConstructorDetails => () => [];

        protected override Func<MemberDetail[]> CreateMemberDetails => () => [new PetIDMemberDetail(locker, LoadMemberInfo), new AmountMemberDetail(locker, LoadMemberInfo)];


        public sealed class PetIDMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, System.Guid>
        {
            public PetIDMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "PetID";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, System.Guid> Getter => (x) => x.PetID;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, System.Guid> Setter => (x, value) => x.PetID = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Commands.FeedPetCommand)x).PetID;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Commands.FeedPetCommand)x).PetID = (System.Guid)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, System.Guid>?> CreateBackingFieldDetail => () => new _PetID_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class AmountMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, int>
        {
            public AmountMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Amount";

            private readonly Type type = typeof(int);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, int> Getter => (x) => x.Amount;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, int> Setter => (x, value) => x.Amount = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Commands.FeedPetCommand)x).Amount;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Commands.FeedPetCommand)x).Amount = (int)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, int>?> CreateBackingFieldDetail => () => new _Amount_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class _PetID_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, System.Guid>
        {
            public _PetID_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<PetID>k__BackingField";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, System.Guid>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Amount_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, int>
        {
            public _Amount_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Amount>k__BackingField";

            private readonly Type type = typeof(int);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.FeedPetCommand, int>?> CreateBackingFieldDetail => () => null;
        }
    }
}