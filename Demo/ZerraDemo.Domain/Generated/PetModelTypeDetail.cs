﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using ZerraDemo.Domain.Pets.Models;

namespace ZerraDemo.Domain.Generated
{
    public class PetModelTypeDetail : TypeDetail<PetModel>
    {
        [ModuleInitializer]
        public static void Initialize() => TypeAnalyzer.InitializeTypeDetail(new PetModelTypeDetail());

        public PetModelTypeDetail() : base(typeof(PetModel)) { }

        public override IReadOnlyList<MethodDetail<PetModel>> MethodDetails => [];
        public override IReadOnlyList<MethodDetail> MethodDetailsBoxed => [];

        private ConstructorDetail<PetModel>[] constructorDetails = [new PetModelConstructorDetail()];
        public override IReadOnlyList<ConstructorDetail<PetModel>> ConstructorDetails => constructorDetails;
        public override IReadOnlyList<ConstructorDetail> ConstructorDetailsBoxed => constructorDetails;

        public override Func<PetModel> Creator => () => new PetModel();
        public override bool HasCreator => true;

        public override bool IsNullable => false;
        public override CoreType? CoreType => null;
        public override SpecialType? SpecialType => null;

        public override Type InnerType => throw new NotSupportedException();

        public override bool IsTask => false;

        public override CoreEnumType? EnumUnderlyingType => null;

        public override IReadOnlyList<Type> BaseTypes => [];

        public override IReadOnlyList<Type> Interfaces => [];

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
        public override bool IsIReadOnlyDictionaryGeneric => throw new NotImplementedException();

        private MemberDetail[]? memberDetails = null;
        public override IReadOnlyList<MemberDetail> MemberDetails
        {
            get
            {
                if (memberDetails is null)
                {
                    lock (locker)
                    {
                        memberDetails ??= [
                             new PetModelMemberDetail_ID(locker),
                             new PetModelMemberDetail_Name(locker),
                             new PetModelMemberDetail_Breed(locker),
                             new PetModelMemberDetail_Species(locker),
                             new PetModelMemberDetail_LastEaten(locker),
                             new PetModelMemberDetail_AmountEaten(locker),
                             new PetModelMemberDetail_LastPooped(locker)
                         ];
                    }
                }
                return memberDetails;
            }
        }

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

        public override Func<object> CreatorBoxed => Creator;
        public override bool HasCreatorBoxed => true;

        public override Delegate? CreatorTyped => Creator;

        public sealed class PetModelConstructorDetail : ConstructorDetail<PetModel>
        {
            public override Func<object?[]?, PetModel> CreatorWithArgs => throw new NotSupportedException();
            public override bool HasCreatorWithArgs => false;

            public override Func<PetModel> Creator => () => new PetModel();
            public override bool HasCreator => true;

            public override ConstructorInfo ConstructorInfo => throw new NotSupportedException();

            public override string Name => "main";

            public override IReadOnlyList<ParameterInfo> ParametersInfo => [];

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object?[]?, PetModel> CreatorWithArgsBoxed => throw new NotSupportedException();
            public override bool HasCreatorWithArgsBoxed => false;

            public override Func<object> CreatorBoxed => () => new PetModel();
            public override bool HasCreatorBoxed => true;

            public override Delegate? CreatorTyped => Creator;
            public override Delegate? CreatorWithArgsTyped => throw new NotSupportedException();
        }

        public sealed class PetModelMemberDetail_ID : MemberDetail<PetModel, Guid>
        {
            public override string Name => nameof(PetModel.ID);

            private readonly Type type = typeof(Guid);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override MemberDetail<PetModel, Guid>? BackingFieldDetail => null;
            public override MemberDetail? BackingFieldDetailBoxed => null;

            public override Func<PetModel, Guid> Getter => (x) => x.ID;
            public override bool HasGetter => true;

            public override Action<PetModel, Guid> Setter => (x, value) => x.ID = value;
            public override bool HasSetter => true;

            public override TypeDetail<Guid> TypeDetail => TypeAnalyzer<Guid>.GetTypeDetail();
            public override TypeDetail TypeDetailBoxed => TypeDetail;

            private MemberInfo? memberInfo = null;
            public override MemberInfo MemberInfo
            {
                get
                {
                    if (memberInfo is null)
                    {
                        lock (locker)
                        {
                            memberInfo ??= typeof(PetModel).GetProperty(Name)!;
                        }
                    }
                    return memberInfo;
                }
            }

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).ID;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).ID = (Guid)value!;
            public override bool HasSetterBoxed => true;

            public override Delegate GetterTyped => Getter;
            public override Delegate SetterTyped => Setter;

            private readonly object locker;
            public PetModelMemberDetail_ID(object locker) => this.locker = locker;
        }
        public sealed class PetModelMemberDetail_Name : MemberDetail<PetModel, string?>
        {
            public override string Name => nameof(PetModel.Name);

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override MemberDetail<PetModel, string?>? BackingFieldDetail => null;
            public override MemberDetail? BackingFieldDetailBoxed => null;

            public override Func<PetModel, string?> Getter => (x) => x.Name;
            public override bool HasGetter => true;

            public override Action<PetModel, string?> Setter => (x, value) => x.Name = value;
            public override bool HasSetter => true;

            public override TypeDetail<string?> TypeDetail => TypeAnalyzer<string?>.GetTypeDetail();
            public override TypeDetail TypeDetailBoxed => TypeDetail;

            private MemberInfo? memberInfo = null;
            public override MemberInfo MemberInfo
            {
                get
                {
                    if (memberInfo is null)
                    {
                        lock (locker)
                        {
                            memberInfo ??= typeof(PetModel).GetProperty(nameof(Name))!;
                        }
                    }
                    return memberInfo;
                }
            }

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).Name;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).Name = (string?)value;
            public override bool HasSetterBoxed => true;

            public override Delegate GetterTyped => Getter;
            public override Delegate SetterTyped => Setter;

            private readonly object locker;
            public PetModelMemberDetail_Name(object locker) => this.locker = locker;
        }
        public sealed class PetModelMemberDetail_Breed : MemberDetail<PetModel, string?>
        {
            public override string Name => nameof(PetModel.Breed);

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override MemberDetail<PetModel, string?>? BackingFieldDetail => null;
            public override MemberDetail? BackingFieldDetailBoxed => null;

            public override Func<PetModel, string?> Getter => (x) => x.Breed;
            public override bool HasGetter => true;

            public override Action<PetModel, string?> Setter => (x, value) => x.Breed = value;
            public override bool HasSetter => true;

            public override TypeDetail<string?> TypeDetail => TypeAnalyzer<string?>.GetTypeDetail();
            public override TypeDetail TypeDetailBoxed => TypeDetail;

            private MemberInfo? memberInfo = null;
            public override MemberInfo MemberInfo
            {
                get
                {
                    if (memberInfo is null)
                    {
                        lock (locker)
                        {
                            memberInfo ??= typeof(PetModel).GetProperty(Name)!;
                        }
                    }
                    return memberInfo;
                }
            }

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).Breed;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).Breed = (string?)value;
            public override bool HasSetterBoxed => true;

            public override Delegate GetterTyped => Getter;
            public override Delegate SetterTyped => Setter;

            private readonly object locker;
            public PetModelMemberDetail_Breed(object locker) => this.locker = locker;
        }
        public sealed class PetModelMemberDetail_Species : MemberDetail<PetModel, string?>
        {
            public override string Name => nameof(PetModel.Species);

            private readonly Type type = typeof(string);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override MemberDetail<PetModel, string?>? BackingFieldDetail => null;
            public override MemberDetail? BackingFieldDetailBoxed => null;

            public override Func<PetModel, string?> Getter => (x) => x.Species;
            public override bool HasGetter => true;

            public override Action<PetModel, string?> Setter => (x, value) => x.Species = value;
            public override bool HasSetter => true;

            public override TypeDetail<string?> TypeDetail => TypeAnalyzer<string?>.GetTypeDetail();
            public override TypeDetail TypeDetailBoxed => TypeDetail;

            private MemberInfo? memberInfo = null;
            public override MemberInfo MemberInfo
            {
                get
                {
                    if (memberInfo is null)
                    {
                        lock (locker)
                        {
                            memberInfo ??= typeof(PetModel).GetProperty(Name)!;
                        }
                    }
                    return memberInfo;
                }
            }

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).Species;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).Species = (string?)value;
            public override bool HasSetterBoxed => true;

            public override Delegate GetterTyped => Getter;
            public override Delegate SetterTyped => Setter;

            private readonly object locker;
            public PetModelMemberDetail_Species(object locker) => this.locker = locker;
        }
        public sealed class PetModelMemberDetail_LastEaten : MemberDetail<PetModel, DateTime?>
        {
            public override string Name => nameof(PetModel.LastEaten);

            private readonly Type type = typeof(DateTime?);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override MemberDetail<PetModel, DateTime?>? BackingFieldDetail => null;
            public override MemberDetail? BackingFieldDetailBoxed => null;

            public override Func<PetModel, DateTime?> Getter => (x) => x.LastEaten;
            public override bool HasGetter => true;

            public override Action<PetModel, DateTime?> Setter => (x, value) => x.LastEaten = value;
            public override bool HasSetter => true;

            public override TypeDetail<DateTime?> TypeDetail => TypeAnalyzer<DateTime?>.GetTypeDetail();
            public override TypeDetail TypeDetailBoxed => TypeDetail;

            private MemberInfo? memberInfo = null;
            public override MemberInfo MemberInfo
            {
                get
                {
                    if (memberInfo is null)
                    {
                        lock (locker)
                        {
                            memberInfo ??= typeof(PetModel).GetProperty(Name)!;
                        }
                    }
                    return memberInfo;
                }
            }

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).LastEaten;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).LastEaten = (DateTime?)value;
            public override bool HasSetterBoxed => true;

            public override Delegate GetterTyped => Getter;
            public override Delegate SetterTyped => Setter;

            private readonly object locker;
            public PetModelMemberDetail_LastEaten(object locker) => this.locker = locker;
        }
        public sealed class PetModelMemberDetail_AmountEaten : MemberDetail<PetModel, int?>
        {
            public override string Name => nameof(PetModel.AmountEaten);

            private readonly Type type = typeof(int?);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override MemberDetail<PetModel, int?>? BackingFieldDetail => null;
            public override MemberDetail? BackingFieldDetailBoxed => null;

            public override Func<PetModel, int?> Getter => (x) => x.AmountEaten;
            public override bool HasGetter => true;

            public override Action<PetModel, int?> Setter => (x, value) => x.AmountEaten = value;
            public override bool HasSetter => true;

            public override TypeDetail<int?> TypeDetail => TypeAnalyzer<int?>.GetTypeDetail();
            public override TypeDetail TypeDetailBoxed => TypeDetail;

            private MemberInfo? memberInfo = null;
            public override MemberInfo MemberInfo
            {
                get
                {
                    if (memberInfo is null)
                    {
                        lock (locker)
                        {
                            memberInfo ??= typeof(PetModel).GetProperty(Name)!;
                        }
                    }
                    return memberInfo;
                }
            }

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).AmountEaten;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).AmountEaten = (int?)value;
            public override bool HasSetterBoxed => true;

            public override Delegate GetterTyped => Getter;
            public override Delegate SetterTyped => Setter;

            private readonly object locker;
            public PetModelMemberDetail_AmountEaten(object locker) => this.locker = locker;
        }
        public sealed class PetModelMemberDetail_LastPooped : MemberDetail<PetModel, DateTime?>
        {
            public override string Name => nameof(PetModel.LastPooped);

            private readonly Type type = typeof(DateTime?);
            public override Type Type => type;

            public override bool IsBacked => true;

            public override MemberDetail<PetModel, DateTime?>? BackingFieldDetail => null;
            public override MemberDetail? BackingFieldDetailBoxed => null;

            public override Func<PetModel, DateTime?> Getter => (x) => x.LastPooped;
            public override bool HasGetter => true;

            public override Action<PetModel, DateTime?> Setter => (x, value) => x.LastPooped = value;
            public override bool HasSetter => true;

            public override TypeDetail<DateTime?> TypeDetail => TypeAnalyzer<DateTime?>.GetTypeDetail();
            public override TypeDetail TypeDetailBoxed => TypeDetail;

            private MemberInfo? memberInfo = null;
            public override MemberInfo MemberInfo
            {
                get
                {
                    if (memberInfo is null)
                    {
                        lock (locker)
                        {
                            memberInfo ??= typeof(PetModel).GetProperty(Name)!;
                        }
                    }
                    return memberInfo;
                }
            }

            public override IReadOnlyList<Attribute> Attributes => [];

            public override Func<object, object?> GetterBoxed => (x) => ((PetModel)x).LastPooped;
            public override bool HasGetterBoxed => true;

            public override Action<object, object?> SetterBoxed => (x, value) => ((PetModel)x).LastPooped = (DateTime?)value;
            public override bool HasSetterBoxed => true;

            public override Delegate GetterTyped => Getter;
            public override Delegate SetterTyped => Setter;

            private readonly object locker;
            public PetModelMemberDetail_LastPooped(object locker) => this.locker = locker;
        }
    }
    public class PetModelArrayTypeDetail : TypeDetail<PetModel[]>
    {
        [ModuleInitializer]
        public static void Initialize() => TypeAnalyzer.InitializeTypeDetail(new PetModelArrayTypeDetail());

        public PetModelArrayTypeDetail() : base(typeof(PetModel[])) { }

        public override IReadOnlyList<MethodDetail<PetModel[]>> MethodDetails => [];
        public override IReadOnlyList<MethodDetail> MethodDetailsBoxed => [];

        public override IReadOnlyList<ConstructorDetail<PetModel[]>> ConstructorDetails => [];
        public override IReadOnlyList<ConstructorDetail> ConstructorDetailsBoxed => ConstructorDetails;

        public override Func<PetModel[]> Creator => throw new NotSupportedException();
        public override bool HasCreator => false;

        public override bool IsNullable => false;
        public override CoreType? CoreType => null;
        public override SpecialType? SpecialType => null;

        private readonly Type innerType = typeof(PetModel);
        public override Type InnerType => innerType;

        public override bool IsTask => false;

        public override CoreEnumType? EnumUnderlyingType => null;

        public override IReadOnlyList<Type> BaseTypes => [];

        public override IReadOnlyList<Type> Interfaces => [];

        public override bool HasIEnumerable => true;
        public override bool HasIEnumerableGeneric => true;
        public override bool HasICollection => true;
        public override bool HasICollectionGeneric => true;
        public override bool HasIReadOnlyCollectionGeneric => true;
        public override bool HasIList => true;
        public override bool HasIListGeneric => true;
        public override bool HasIReadOnlyListGeneric => true;
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
        public override bool IsIReadOnlyDictionaryGeneric => throw new NotImplementedException();

        public override IReadOnlyList<MemberDetail> MemberDetails => [];

        public override IReadOnlyList<Attribute> Attributes => [];

        private IReadOnlyList<Type>? innerTypes = null;
        public override IReadOnlyList<Type> InnerTypes
        {
            get
            {
                if (innerTypes is null)
                {
                    lock (locker)
                    {
                        innerTypes ??= [typeof(PetModel)];
                    }
                }
                return innerTypes;
            }
        }

        private IReadOnlyList<TypeDetail>? innerTypeDetails = null;
        public override IReadOnlyList<TypeDetail> InnerTypeDetails
        {
            get
            {
                if (innerTypeDetails is null)
                {
                    lock (locker)
                    {
                        innerTypeDetails ??= [TypeAnalyzer<PetModel>.GetTypeDetail()];
                    }
                }
                return innerTypeDetails;
            }
        }

        private TypeDetail? innerTypeDetail = null;
        public override TypeDetail InnerTypeDetail
        {
            get
            {
                if (innerTypeDetail is null)
                {
                    lock (locker)
                    {
                        innerTypeDetail ??= InnerTypeDetails[0];
                    }
                }
                return innerTypeDetail;
            }
        }

        public override Type IEnumerableGenericInnerType => throw new NotSupportedException();
        public override TypeDetail IEnumerableGenericInnerTypeDetail
        {
            get
            {
                if (innerTypeDetail is null)
                {
                    lock (locker)
                    {
                        innerTypeDetail ??= InnerTypeDetails[0];
                    }
                }
                return innerTypeDetail;
            }
        }

        public override Type DictionaryInnerType => throw new NotSupportedException();
        public override TypeDetail DictionaryInnerTypeDetail => throw new NotSupportedException();

        public override Func<object, object?> TaskResultGetter => throw new NotSupportedException();
        public override bool HasTaskResultGetter => false;

        public override Func<object> CreatorBoxed => Creator;
        public override bool HasCreatorBoxed => true;

        public override Delegate? CreatorTyped => Creator;
    }
}
