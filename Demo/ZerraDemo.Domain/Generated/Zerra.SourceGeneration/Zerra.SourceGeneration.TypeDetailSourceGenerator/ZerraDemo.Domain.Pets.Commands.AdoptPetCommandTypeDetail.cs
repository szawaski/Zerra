//Zerra Generated File

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Reflection.Generation;
using ZerraDemo.Domain.Pets.Commands;

namespace ZerraDemo.Domain.Pets.Commands.SourceGeneration
{
    public sealed class AdoptPetCommandTypeDetail : TypeDetailTGenerationBase<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand>
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

        public override Func<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand> Creator => () => new ZerraDemo.Domain.Pets.Commands.AdoptPetCommand();
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

        public override Func<object> CreatorBoxed => () => new ZerraDemo.Domain.Pets.Commands.AdoptPetCommand();
        public override bool HasCreatorBoxed => true;

        protected override Func<MethodDetail<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand>[]> CreateMethodDetails => () => [];

        protected override Func<ConstructorDetail<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand>[]> CreateConstructorDetails => () => [];

        protected override Func<MemberDetail[]> CreateMemberDetails => () => [new PetIDMemberDetail(locker, LoadMemberInfo), new BreedIDMemberDetail(locker, LoadMemberInfo), new NameMemberDetail(locker, LoadMemberInfo)];


        public sealed class PetIDMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid>
        {
            public PetIDMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "PetID";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid> Getter => (x) => x.PetID;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid> Setter => (x, value) => x.PetID = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Commands.AdoptPetCommand)x).PetID;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Commands.AdoptPetCommand)x).PetID = (System.Guid)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid>?> CreateBackingFieldDetail => () => new _PetID_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class BreedIDMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid>
        {
            public BreedIDMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "BreedID";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid> Getter => (x) => x.BreedID;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid> Setter => (x, value) => x.BreedID = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Commands.AdoptPetCommand)x).BreedID;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Commands.AdoptPetCommand)x).BreedID = (System.Guid)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid>?> CreateBackingFieldDetail => () => new _BreedID_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class NameMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, string?>
        {
            public NameMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Name";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, string?> Getter => (x) => x.Name;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, string?> Setter => (x, value) => x.Name = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Commands.AdoptPetCommand)x).Name;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Commands.AdoptPetCommand)x).Name = (string?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, string?>?> CreateBackingFieldDetail => () => new _Name_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class _PetID_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid>
        {
            public _PetID_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<PetID>k__BackingField";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _BreedID_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid>
        {
            public _BreedID_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<BreedID>k__BackingField";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, System.Guid>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Name_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, string?>
        {
            public _Name_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Name>k__BackingField";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Commands.AdoptPetCommand, string?>?> CreateBackingFieldDetail => () => null;
        }
    }
}