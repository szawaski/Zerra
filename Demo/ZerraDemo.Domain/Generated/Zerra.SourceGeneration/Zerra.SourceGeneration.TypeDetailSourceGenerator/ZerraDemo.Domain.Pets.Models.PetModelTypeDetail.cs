//Zerra Generated File

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Reflection.Generation;
using ZerraDemo.Domain.Pets.Models;

namespace ZerraDemo.Domain.Pets.Models.SourceGeneration
{
    public sealed class PetModelTypeDetail : TypeDetailTGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel>
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

        public override Func<ZerraDemo.Domain.Pets.Models.PetModel> Creator => () => new ZerraDemo.Domain.Pets.Models.PetModel();
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

        public override Func<object> CreatorBoxed => () => new ZerraDemo.Domain.Pets.Models.PetModel();
        public override bool HasCreatorBoxed => true;

        protected override Func<MethodDetail<ZerraDemo.Domain.Pets.Models.PetModel>[]> CreateMethodDetails => () => [];

        protected override Func<ConstructorDetail<ZerraDemo.Domain.Pets.Models.PetModel>[]> CreateConstructorDetails => () => [];

        protected override Func<MemberDetail[]> CreateMemberDetails => () => [new IDMemberDetail(locker, LoadMemberInfo), new NameMemberDetail(locker, LoadMemberInfo), new BreedMemberDetail(locker, LoadMemberInfo), new SpeciesMemberDetail(locker, LoadMemberInfo), new LastEatenMemberDetail(locker, LoadMemberInfo), new AmountEatenMemberDetail(locker, LoadMemberInfo), new LastPoopedMemberDetail(locker, LoadMemberInfo)];


        public sealed class IDMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, System.Guid>
        {
            public IDMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "ID";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Models.PetModel, System.Guid> Getter => (x) => x.ID;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Models.PetModel, System.Guid> Setter => (x, value) => x.ID = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).ID;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).ID = (System.Guid)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, System.Guid>?> CreateBackingFieldDetail => () => new _ID_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class NameMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, string?>
        {
            public NameMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Name";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Models.PetModel, string?> Getter => (x) => x.Name;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Models.PetModel, string?> Setter => (x, value) => x.Name = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).Name;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).Name = (string?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, string?>?> CreateBackingFieldDetail => () => new _Name_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class BreedMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, string?>
        {
            public BreedMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Breed";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Models.PetModel, string?> Getter => (x) => x.Breed;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Models.PetModel, string?> Setter => (x, value) => x.Breed = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).Breed;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).Breed = (string?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, string?>?> CreateBackingFieldDetail => () => new _Breed_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class SpeciesMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, string?>
        {
            public SpeciesMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Species";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Models.PetModel, string?> Getter => (x) => x.Species;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Models.PetModel, string?> Setter => (x, value) => x.Species = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).Species;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).Species = (string?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, string?>?> CreateBackingFieldDetail => () => new _Species_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class LastEatenMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?>
        {
            public LastEatenMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "LastEaten";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?> Getter => (x) => x.LastEaten;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?> Setter => (x, value) => x.LastEaten = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).LastEaten;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).LastEaten = (System.DateTime?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?>?> CreateBackingFieldDetail => () => new _LastEaten_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class AmountEatenMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, int?>
        {
            public AmountEatenMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "AmountEaten";

            private readonly Type type = typeof(int);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Models.PetModel, int?> Getter => (x) => x.AmountEaten;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Models.PetModel, int?> Setter => (x, value) => x.AmountEaten = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).AmountEaten;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).AmountEaten = (int?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, int?>?> CreateBackingFieldDetail => () => new _AmountEaten_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class LastPoopedMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?>
        {
            public LastPoopedMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "LastPooped";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?> Getter => (x) => x.LastPooped;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?> Setter => (x, value) => x.LastPooped = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).LastPooped;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Pets.Models.PetModel)x).LastPooped = (System.DateTime?)value!;
            public override bool HasSetterBoxed => true;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?>?> CreateBackingFieldDetail => () => new _LastPooped_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class _ID_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, System.Guid>
        {
            public _ID_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<ID>k__BackingField";

            private readonly Type type = typeof(System.Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, System.Guid>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Name_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, string?>
        {
            public _Name_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Name>k__BackingField";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, string?>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Breed_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, string?>
        {
            public _Breed_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Breed>k__BackingField";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, string?>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Species_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, string?>
        {
            public _Species_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Species>k__BackingField";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, string?>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _LastEaten_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?>
        {
            public _LastEaten_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<LastEaten>k__BackingField";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _AmountEaten_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, int?>
        {
            public _AmountEaten_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<AmountEaten>k__BackingField";

            private readonly Type type = typeof(int);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, int?>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _LastPooped_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?>
        {
            public _LastPooped_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<LastPooped>k__BackingField";

            private readonly Type type = typeof(System.DateTime);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Models.PetModel, System.DateTime?>?> CreateBackingFieldDetail => () => null;
        }
    }
}