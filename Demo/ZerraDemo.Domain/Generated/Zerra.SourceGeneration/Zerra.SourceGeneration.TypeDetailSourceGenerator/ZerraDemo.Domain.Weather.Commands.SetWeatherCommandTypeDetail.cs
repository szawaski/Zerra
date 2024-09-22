//Zerra Generated File

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Reflection.Generation;
using ZerraDemo.Domain.Weather.Commands;

namespace ZerraDemo.Domain.Weather.Commands.SourceGeneration
{
    public sealed class SetWeatherCommandTypeDetail : TypeDetailTGenerationBase<ZerraDemo.Domain.Weather.Commands.SetWeatherCommand>
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

        public override Func<ZerraDemo.Domain.Weather.Commands.SetWeatherCommand> Creator => () => new ZerraDemo.Domain.Weather.Commands.SetWeatherCommand();
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

        public override Func<object> CreatorBoxed => () => new ZerraDemo.Domain.Weather.Commands.SetWeatherCommand();
        public override bool HasCreatorBoxed => true;

        protected override Func<MethodDetail<ZerraDemo.Domain.Weather.Commands.SetWeatherCommand>[]> CreateMethodDetails => () => [];

        protected override Func<ConstructorDetail<ZerraDemo.Domain.Weather.Commands.SetWeatherCommand>[]> CreateConstructorDetails => () => [];

        protected override Func<MemberDetail[]> CreateMemberDetails => () => [new WeatherTypeMemberDetail(locker, LoadMemberInfo)];


        public sealed class WeatherTypeMemberDetail : MemberDetailGenerationBase<ZerraDemo.Domain.Weather.Commands.SetWeatherCommand, ZerraDemo.Domain.Weather.Constants.WeatherType>
        {
            public WeatherTypeMemberDetail(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

            public override string Name => "WeatherType";

            private readonly Type type = typeof(ZerraDemo.Domain.Weather.Constants.WeatherType);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override Func<ZerraDemo.Domain.Weather.Commands.SetWeatherCommand, ZerraDemo.Domain.Weather.Constants.WeatherType> Getter => (x) => x.WeatherType;
            public override bool HasGetter => true;

            public override Action<ZerraDemo.Domain.Weather.Commands.SetWeatherCommand, ZerraDemo.Domain.Weather.Constants.WeatherType> Setter => (x, value) => x.WeatherType = value;
            public override bool HasSetter => true;

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((ZerraDemo.Domain.Weather.Commands.SetWeatherCommand)x).WeatherType;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((ZerraDemo.Domain.Weather.Commands.SetWeatherCommand)x).WeatherType = (ZerraDemo.Domain.Weather.Constants.WeatherType)value!;
            public override bool HasSetterBoxed => true;
        }
    }
}