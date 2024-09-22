//Zerra Generated File

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Reflection.Generation;
using ZerraDemo.Domain.Pets.Exceptions;

namespace ZerraDemo.Domain.Pets.Exceptions.SourceGeneration
{
    public sealed class PoopExceptionTypeDetail : TypeDetailTGenerationBase<ZerraDemo.Domain.Pets.Exceptions.PoopException>
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

        public override Func<ZerraDemo.Domain.Pets.Exceptions.PoopException> Creator => () => new ZerraDemo.Domain.Pets.Exceptions.PoopException();
        public override bool HasCreator => true;

        public override bool IsNullable => false;

        public override CoreType? CoreType => null;
        public override SpecialType? SpecialType => null;
        public override CoreEnumType? EnumUnderlyingType => null;

        private readonly Type? innerType = null;
        public override Type InnerType => innerType ?? throw new NotSupportedException();

        public override bool IsTask => false;

        public override IReadOnlyList<Type> BaseTypes => [typeof(System.Exception), typeof(System.Object)];

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

        public override Func<object> CreatorBoxed => () => new ZerraDemo.Domain.Pets.Exceptions.PoopException();
        public override bool HasCreatorBoxed => true;

        protected override Func<MethodDetail<ZerraDemo.Domain.Pets.Exceptions.PoopException>[]> CreateMethodDetails => () => [];

        protected override Func<ConstructorDetail<ZerraDemo.Domain.Pets.Exceptions.PoopException>[]> CreateConstructorDetails => () => [];

        protected override Func<MemberDetail[]> CreateMemberDetails => () => [new PetNameMemberDetail(locker, LoadMemberInfo), new WeatherMemberDetail(locker, LoadMemberInfo)];


        public sealed class PetNameMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Exceptions.PoopException, string?>
        {
            public PetNameMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "PetName";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Exceptions.PoopException, string?> Getter => (x) => x.PetName;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Exceptions.PoopException, string?> Setter => throw new NotSupportedException();
            public override bool HasSetter => false;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Exceptions.PoopException)x).PetName;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => throw new NotSupportedException();
            public override bool HasSetterBoxed => false;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Exceptions.PoopException, string?>?> CreateBackingFieldDetail => () => new _PetName_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class WeatherMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Pets.Exceptions.PoopException, ZerraDemo.Domain.Weather.Constants.WeatherType?>
        {
            public WeatherMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "Weather";

            private readonly Type type = typeof(ZerraDemo.Domain.Weather.Constants.WeatherType);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Pets.Exceptions.PoopException, ZerraDemo.Domain.Weather.Constants.WeatherType?> Getter => (x) => x.Weather;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Pets.Exceptions.PoopException, ZerraDemo.Domain.Weather.Constants.WeatherType?> Setter => throw new NotSupportedException();
            public override bool HasSetter => false;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Pets.Exceptions.PoopException)x).Weather;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => throw new NotSupportedException();
            public override bool HasSetterBoxed => false;

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Exceptions.PoopException, ZerraDemo.Domain.Weather.Constants.WeatherType?>?> CreateBackingFieldDetail => () => new _Weather_k__BackingFieldMemberDetail(locker, loadMemberInfo);
        }
        public sealed class _PetName_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Exceptions.PoopException, string?>
        {
            public _PetName_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<PetName>k__BackingField";

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Exceptions.PoopException, string?>?> CreateBackingFieldDetail => () => null;
        }
        public sealed class _Weather_k__BackingFieldMemberDetail : PrivateMemberDetailGenerationBase<ZerraDemo.Domain.Pets.Exceptions.PoopException, ZerraDemo.Domain.Weather.Constants.WeatherType?>
        {
            public _Weather_k__BackingFieldMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "<Weather>k__BackingField";

            private readonly Type type = typeof(ZerraDemo.Domain.Weather.Constants.WeatherType);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            protected override Func<MemberDetail<ZerraDemo.Domain.Pets.Exceptions.PoopException, ZerraDemo.Domain.Weather.Constants.WeatherType?>?> CreateBackingFieldDetail => () => null;
        }
    }
}