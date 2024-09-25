//using System;
//using System.Collections.Generic;
//using System.Runtime.CompilerServices;
//using Zerra.Reflection;
//using Zerra.Reflection.Generation;
//using ZerraDemo.Domain.Pets.Models;

//namespace ZerraDemo.Domain.GeneratedTemp
//{
//    public class PetModelTypeDetail : TypeDetailTGenerationBase<PetModel>
//    {
//        [ModuleInitializer]
//        public static void Initialize() => TypeAnalyzer.AddTypeDetailCreator(typeof(PetModel), () => new PetModelTypeDetail());

//        protected override Func<MethodDetail<PetModel>[]> CreateMethodDetails => () => [];

//        protected override Func<ConstructorDetail<PetModel>[]> CreateConstructorDetails => () => [
//            new PetModelConstructorDetail(locker, LoadConstructorInfo)
//        ];

//        protected override Func<MemberDetail[]> CreateMemberDetails => () => [
//            new PetModelMemberDetail_ID(locker, LoadMemberInfo),
//            new PetModelMemberDetail_Name(locker, LoadMemberInfo),
//            new PetModelMemberDetail_Breed(locker, LoadMemberInfo),
//            new PetModelMemberDetail_Species(locker, LoadMemberInfo),
//            new PetModelMemberDetail_LastEaten(locker, LoadMemberInfo),
//            new PetModelMemberDetail_AmountEaten(locker, LoadMemberInfo),
//            new PetModelMemberDetail_LastPooped(locker, LoadMemberInfo)
//        ];

//        public override bool HasIEnumerable => false;
//        public override bool HasIEnumerableGeneric => false;
//        public override bool HasICollection => false;
//        public override bool HasICollectionGeneric => false;
//        public override bool HasIReadOnlyCollectionGeneric => false;
//        public override bool HasIList => false;
//        public override bool HasIListGeneric => false;
//        public override bool HasIReadOnlyListGeneric => false;
//        public override bool HasISetGeneric => false;
//        public override bool HasIReadOnlySetGeneric => false;
//        public override bool HasIDictionary => false;
//        public override bool HasIDictionaryGeneric => false;
//        public override bool HasIReadOnlyDictionaryGeneric => false;

//        public override bool IsIEnumerable => false;
//        public override bool IsIEnumerableGeneric => false;
//        public override bool IsICollection => false;
//        public override bool IsICollectionGeneric => false;
//        public override bool IsIReadOnlyCollectionGeneric => false;
//        public override bool IsIList => false;
//        public override bool IsIListGeneric => false;
//        public override bool IsIReadOnlyListGeneric => false;
//        public override bool IsISetGeneric => false;
//        public override bool IsIReadOnlySetGeneric => false;
//        public override bool IsIDictionary => false;
//        public override bool IsIDictionaryGeneric => false;
//        public override bool IsIReadOnlyDictionaryGeneric => false;

//        public override Func<PetModel> Creator => () => new PetModel();
//        public override bool HasCreator => true;

//        public override bool IsNullable => false;
//        public override CoreType? CoreType => null;
//        public override SpecialType? SpecialType => null;
//        public override CoreEnumType? EnumUnderlyingType => null;

//        public override Type InnerType => throw new NotSupportedException();

//        public override bool IsTask => false;

//        public override IReadOnlyList<Type> BaseTypes => [];

//        public override IReadOnlyList<Type> Interfaces => [];

//        protected override Func<Attribute[]> CreateAttributes => () => [];

//        public override IReadOnlyList<Type> InnerTypes => [];
//        public override IReadOnlyList<TypeDetail> InnerTypeDetails => [];

//        public override TypeDetail InnerTypeDetail => throw new NotSupportedException();

//        public override Type IEnumerableGenericInnerType => throw new NotSupportedException();
//        public override TypeDetail IEnumerableGenericInnerTypeDetail => throw new NotSupportedException();

//        public override Type DictionaryInnerType => throw new NotSupportedException();
//        public override TypeDetail DictionaryInnerTypeDetail => throw new NotSupportedException();

//        public override Func<object, object?> TaskResultGetter => throw new NotSupportedException();
//        public override bool HasTaskResultGetter => false;

//        public override Func<object> CreatorBoxed => Creator;
//        public override bool HasCreatorBoxed => true;

//        public sealed class PetModelConstructorDetail : ConstructorDetailGenerationBase<PetModel>
//        {
//            public PetModelConstructorDetail(object locker, Action loadConstructorInfo) : base(locker, loadConstructorInfo) { }

//            public override Func<object?[]?, PetModel> CreatorWithArgs => throw new NotSupportedException();
//            public override bool HasCreatorWithArgs => false;

//            public override Func<PetModel> Creator => () => new PetModel();
//            public override bool HasCreator => true;

//            public override string Name => ".ctor";

//            protected override Func<ParameterDetail[]> CreateParameters => () => [];

//            protected override Func<Attribute[]> CreateAttributes => () => [];

//            public override Func<object?[]?, PetModel> CreatorWithArgsBoxed => throw new NotSupportedException();
//            public override bool HasCreatorWithArgsBoxed => false;

//            public override Func<object> CreatorBoxed => () => new PetModel();
//            public override bool HasCreatorBoxed => true;

//            public override Delegate? CreatorTyped => Creator;
//            public override Delegate? CreatorWithArgsTyped => null;
//        }

//        public sealed class PetModelMemberDetail_ID : MemberDetailGenerationBase<PetModel, Guid>
//        {
//            public PetModelMemberDetail_ID(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

//            public override string Name => nameof(PetModel.ID);

//            private readonly Type type = typeof(Guid);
//            public override Type Type => type;

//            public override bool IsBacked => true;

//            public override Func<PetModel, Guid> Getter => (x) => x.ID;
//            public override bool HasGetter => true;

//            public override Action<PetModel, Guid> Setter => (x, value) => x.ID = value;
//            public override bool HasSetter => true;

//            protected override Func<Attribute[]> CreateAttributes => () => [];

//            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).ID;
//            public override bool HasGetterBoxed => true;

//            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).ID = (Guid)value!;
//            public override bool HasSetterBoxed => true;

//            protected override Func<MemberDetail<PetModel, Guid>?> CreateBackingFieldDetail => () => null;
//        }
//        public sealed class PetModelMemberDetail_Name : MemberDetailGenerationBase<PetModel, string?>
//        {
//            public PetModelMemberDetail_Name(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

//            public override string Name => nameof(PetModel.Name);

//            private readonly Type type = typeof(string);
//            public override Type Type => type;

//            public override bool IsBacked => true;

//            public override Func<PetModel, string?> Getter => (x) => x.Name;
//            public override bool HasGetter => true;

//            public override Action<PetModel, string?> Setter => (x, value) => x.Name = value;
//            public override bool HasSetter => true;

//            protected override Func<Attribute[]> CreateAttributes => () => [];

//            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).Name;
//            public override bool HasGetterBoxed => true;

//            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).Name = (string?)value;
//            public override bool HasSetterBoxed => true;

//            protected override Func<MemberDetail<PetModel, string>?> CreateBackingFieldDetail => () => null;
//        }
//        public sealed class PetModelMemberDetail_Breed : MemberDetailGenerationBase<PetModel, string?>
//        {
//            public PetModelMemberDetail_Breed(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

//            public override string Name => nameof(PetModel.Breed);

//            private readonly Type type = typeof(string);
//            public override Type Type => type;

//            public override bool IsBacked => true;

//            public override Func<PetModel, string?> Getter => (x) => x.Breed;
//            public override bool HasGetter => true;

//            public override Action<PetModel, string?> Setter => (x, value) => x.Breed = value;
//            public override bool HasSetter => true;

//            protected override Func<Attribute[]> CreateAttributes => () => [];

//            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).Breed;
//            public override bool HasGetterBoxed => true;

//            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).Breed = (string?)value;
//            public override bool HasSetterBoxed => true;

//            protected override Func<MemberDetail<PetModel, string>?> CreateBackingFieldDetail => () => null;
//        }
//        public sealed class PetModelMemberDetail_Species : MemberDetailGenerationBase<PetModel, string?>
//        {
//            public PetModelMemberDetail_Species(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

//            public override string Name => nameof(PetModel.Species);

//            private readonly Type type = typeof(string);
//            public override Type Type => type;

//            public override bool IsBacked => true;

//            public override Func<PetModel, string?> Getter => (x) => x.Species;
//            public override bool HasGetter => true;

//            public override Action<PetModel, string?> Setter => (x, value) => x.Species = value;
//            public override bool HasSetter => true;

//            protected override Func<Attribute[]> CreateAttributes => () => [];

//            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).Species;
//            public override bool HasGetterBoxed => true;

//            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).Species = (string?)value;
//            public override bool HasSetterBoxed => true;

//            protected override Func<MemberDetail<PetModel, string>?> CreateBackingFieldDetail => () => null;
//        }
//        public sealed class PetModelMemberDetail_LastEaten : MemberDetailGenerationBase<PetModel, DateTime?>
//        {
//            public PetModelMemberDetail_LastEaten(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

//            public override string Name => nameof(PetModel.LastEaten);

//            private readonly Type type = typeof(DateTime?);
//            public override Type Type => type;

//            public override bool IsBacked => true;

//            public override Func<PetModel, DateTime?> Getter => (x) => x.LastEaten;
//            public override bool HasGetter => true;

//            public override Action<PetModel, DateTime?> Setter => (x, value) => x.LastEaten = value;
//            public override bool HasSetter => true;

//            protected override Func<Attribute[]> CreateAttributes => () => [];

//            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).LastEaten;
//            public override bool HasGetterBoxed => true;

//            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).LastEaten = (DateTime?)value;
//            public override bool HasSetterBoxed => true;

//            protected override Func<MemberDetail<PetModel, DateTime?>?> CreateBackingFieldDetail => () => null;
//        }
//        public sealed class PetModelMemberDetail_AmountEaten : MemberDetailGenerationBase<PetModel, int?>
//        {
//            public PetModelMemberDetail_AmountEaten(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

//            public override string Name => nameof(PetModel.AmountEaten);

//            private readonly Type type = typeof(int?);
//            public override Type Type => type;

//            public override bool IsBacked => true;

//            public override Func<PetModel, int?> Getter => (x) => x.AmountEaten;
//            public override bool HasGetter => true;

//            public override Action<PetModel, int?> Setter => (x, value) => x.AmountEaten = value;
//            public override bool HasSetter => true;

//            protected override Func<Attribute[]> CreateAttributes => () => [];

//            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).AmountEaten;
//            public override bool HasGetterBoxed => true;

//            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).AmountEaten = (int?)value;
//            public override bool HasSetterBoxed => true;

//            protected override Func<MemberDetail<PetModel, int?>?> CreateBackingFieldDetail => () => null;
//        }
//        public sealed class PetModelMemberDetail_LastPooped : MemberDetailGenerationBase<PetModel, DateTime?>
//        {
//            public PetModelMemberDetail_LastPooped(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

//            public override string Name => nameof(PetModel.LastPooped);

//            private readonly Type type = typeof(DateTime?);
//            public override Type Type => type;

//            public override bool IsBacked => true;

//            public override Func<PetModel, DateTime?> Getter => (x) => x.LastPooped;
//            public override bool HasGetter => true;

//            public override Action<PetModel, DateTime?> Setter => (x, value) => x.LastPooped = value;
//            public override bool HasSetter => true;

//            protected override Func<Attribute[]> CreateAttributes => () => [];

//            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).LastPooped;
//            public override bool HasGetterBoxed => true;

//            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).LastPooped = (DateTime?)value;
//            public override bool HasSetterBoxed => true;

//            protected override Func<MemberDetail<PetModel, DateTime?>?> CreateBackingFieldDetail => () => null;
//        }
//    }
//    public class PetModelArrayTypeDetail : TypeDetailTGenerationBase<PetModel[]>
//    {
//        [ModuleInitializer]
//        public static void Initialize() => TypeAnalyzer.AddTypeDetailCreator(typeof(PetModel[]), () => new PetModelArrayTypeDetail());

//        protected override Func<MethodDetail<PetModel[]>[]> CreateMethodDetails => () => [];

//        protected override Func<ConstructorDetail<PetModel[]>[]> CreateConstructorDetails => () => [];

//        protected override Func<MemberDetail[]> CreateMemberDetails => () => [];

//        public override bool HasIEnumerable => true;
//        public override bool HasIEnumerableGeneric => true;
//        public override bool HasICollection => true;
//        public override bool HasICollectionGeneric => true;
//        public override bool HasIReadOnlyCollectionGeneric => true;
//        public override bool HasIList => true;
//        public override bool HasIListGeneric => true;
//        public override bool HasIReadOnlyListGeneric => true;
//        public override bool HasISetGeneric => false;
//        public override bool HasIReadOnlySetGeneric => false;
//        public override bool HasIDictionary => false;
//        public override bool HasIDictionaryGeneric => false;
//        public override bool HasIReadOnlyDictionaryGeneric => false;

//        public override bool IsIEnumerable => false;
//        public override bool IsIEnumerableGeneric => false;
//        public override bool IsICollection => false;
//        public override bool IsICollectionGeneric => false;
//        public override bool IsIReadOnlyCollectionGeneric => false;
//        public override bool IsIList => false;
//        public override bool IsIListGeneric => false;
//        public override bool IsIReadOnlyListGeneric => false;
//        public override bool IsISetGeneric => false;
//        public override bool IsIReadOnlySetGeneric => false;
//        public override bool IsIDictionary => false;
//        public override bool IsIDictionaryGeneric => false;
//        public override bool IsIReadOnlyDictionaryGeneric => throw new NotImplementedException();

//        public override Func<PetModel[]> Creator => throw new NotSupportedException();
//        public override bool HasCreator => false;

//        public override bool IsNullable => false;
//        public override CoreType? CoreType => null;
//        public override SpecialType? SpecialType => null;

//        private readonly Type innerType = typeof(PetModel);
//        public override Type InnerType => innerType;

//        public override bool IsTask => false;

//        public override CoreEnumType? EnumUnderlyingType => null;

//        public override IReadOnlyList<Type> BaseTypes => [];

//        public override IReadOnlyList<Type> Interfaces => [];

//        protected override Func<Attribute[]> CreateAttributes => () => [];

//        private IReadOnlyList<Type>? innerTypes = null;
//        public override IReadOnlyList<Type> InnerTypes
//        {
//            get
//            {
//                if (innerTypes is null)
//                {
//                    lock (locker)
//                    {
//                        innerTypes ??= [typeof(PetModel)];
//                    }
//                }
//                return innerTypes;
//            }
//        }

//        private IReadOnlyList<TypeDetail>? innerTypeDetails = null;
//        public override IReadOnlyList<TypeDetail> InnerTypeDetails
//        {
//            get
//            {
//                if (innerTypeDetails is null)
//                {
//                    lock (locker)
//                    {
//                        innerTypeDetails ??= [TypeAnalyzer<PetModel>.GetTypeDetail()];
//                    }
//                }
//                return innerTypeDetails;
//            }
//        }

//        private TypeDetail? innerTypeDetail = null;
//        public override TypeDetail InnerTypeDetail
//        {
//            get
//            {
//                if (innerTypeDetail is null)
//                {
//                    lock (locker)
//                    {
//                        innerTypeDetail ??= InnerTypeDetails[0];
//                    }
//                }
//                return innerTypeDetail;
//            }
//        }

//        public override Type IEnumerableGenericInnerType => throw new NotSupportedException();
//        public override TypeDetail IEnumerableGenericInnerTypeDetail
//        {
//            get
//            {
//                if (innerTypeDetail is null)
//                {
//                    lock (locker)
//                    {
//                        innerTypeDetail ??= InnerTypeDetails[0];
//                    }
//                }
//                return innerTypeDetail;
//            }
//        }

//        public override Type DictionaryInnerType => throw new NotSupportedException();
//        public override TypeDetail DictionaryInnerTypeDetail => throw new NotSupportedException();

//        public override Func<object, object?> TaskResultGetter => throw new NotSupportedException();
//        public override bool HasTaskResultGetter => false;

//        public override Func<object> CreatorBoxed => Creator;
//        public override bool HasCreatorBoxed => true;
//    }
//}
