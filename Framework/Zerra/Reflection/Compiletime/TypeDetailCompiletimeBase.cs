using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Zerra.Reflection.Compiletime
{
    public abstract class TypeDetailCompiletimeBase : TypeDetail
    {
        public TypeDetailCompiletimeBase(Type type) : base(type) { }

        public override sealed bool IsGenerated => true;

        private TypeDetail[]? innerTypesDetails = null;
        public override sealed IReadOnlyList<TypeDetail> InnerTypeDetails
        {
            get
            {
                if (innerTypesDetails is null)
                {
                    lock (locker)
                    {
                        if (innerTypesDetails is null)
                        {
                            var innerTypesRef = InnerTypes;
                            if (innerTypesRef is not null)
                            {
                                var items = new TypeDetail[innerTypesRef.Count];
                                for (var i = 0; i < innerTypesRef.Count; i++)
                                {
                                    items[i] = TypeAnalyzer.GetTypeDetail(innerTypesRef[i]);
                                }
                                innerTypesDetails = items;
                            }
                            else
                            {
                                innerTypesDetails = Array.Empty<TypeDetail>();
                            }
                        }
                    }
                }
                return innerTypesDetails;
            }
        }

        private bool innerTypeDetailLoaded = false;
        private TypeDetail? innerTypesDetail = null;
        public override sealed TypeDetail InnerTypeDetail
        {
            get
            {
                if (!innerTypeDetailLoaded)
                {
                    lock (locker)
                    {
                        if (!innerTypeDetailLoaded)
                        {
                            if (InnerTypes.Count == 1)
                            {
                                innerTypesDetail = InnerType.GetTypeDetail();
                            }
                        }
                        innerTypeDetailLoaded = true;
                    }
                }
                return innerTypesDetail ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(InnerTypeDetail)}");
            }
        }

        private TypeDetail? iEnumerableGenericInnerTypeDetail = null;
        public override sealed TypeDetail IEnumerableGenericInnerTypeDetail
        {
            get
            {
                if (IEnumerableGenericInnerType is null)
                    throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not an IEnumerableGeneric");
                if (iEnumerableGenericInnerTypeDetail is null)
                {
                    lock (locker)
                    {
                        iEnumerableGenericInnerTypeDetail ??= TypeAnalyzer.GetTypeDetail(IEnumerableGenericInnerType);
                    }
                }
                return iEnumerableGenericInnerTypeDetail;
            }
        }

        private bool dictionaryInnerTypeDetailLoaded = false;
        private TypeDetail? dictionaryInnerTypesDetail = null;
        public override sealed TypeDetail DictionaryInnerTypeDetail
        {
            get
            {
                if (!dictionaryInnerTypeDetailLoaded)
                {
                    lock (locker)
                    {
                        if (!dictionaryInnerTypeDetailLoaded)
                        {
                            dictionaryInnerTypesDetail = DictionaryInnerType.GetTypeDetail();
                        }
                        innerTypeDetailLoaded = true;
                    }
                }
                return dictionaryInnerTypesDetail ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(DictionaryInnerTypeDetail)}");
            }
        }

        private static readonly Type keyValuePairType = typeof(KeyValuePair<,>);
        private static readonly Type dictionaryEntryType = typeof(DictionaryEntry);
        private bool dictionartyInnerTypeLoaded = false;
        private Type? dictionaryInnerType = null;
        public override sealed Type DictionaryInnerType
        {
            get
            {
                if (!dictionartyInnerTypeLoaded)
                {
                    lock (locker)
                    {
                        if (!dictionartyInnerTypeLoaded)
                        {
                            if (HasIDictionaryGeneric || HasIReadOnlyDictionaryGeneric)
                            {
                                dictionaryInnerType = TypeAnalyzer.GetGenericType(keyValuePairType, (Type[])InnerTypes);
                            }
                            else if (HasIDictionary)
                            {
                                dictionaryInnerType = dictionaryEntryType;
                            }
                            dictionartyInnerTypeLoaded = true;
                        }
                    }
                }
                return dictionaryInnerType ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(DictionaryInnerType)}");
            }
        }

        protected abstract Func<MethodDetail[]> CreateMethodDetails { get; }
        private MethodDetail[]? methodDetails = null;
        public override sealed IReadOnlyList<MethodDetail> MethodDetailsBoxed
        {
            get
            {
                if (methodDetails is null)
                {
                    lock (locker)
                    {
                        methodDetails ??= CreateMethodDetails();
                    }
                }
                return methodDetails;
            }
        }

        protected abstract Func<ConstructorDetail[]> CreateConstructorDetails { get; }
        private ConstructorDetail[]? constructorDetails = null;
        public override sealed IReadOnlyList<ConstructorDetail> ConstructorDetailsBoxed
        {
            get
            {
                if (constructorDetails is null)
                {
                    lock (locker)
                    {
                        constructorDetails ??= CreateConstructorDetails();
                    }
                }
                return constructorDetails;
            }
        }

        protected abstract Func<MemberDetail[]> CreateMemberDetails { get; }
        private MemberDetail[]? memberDetails = null;
        public override sealed IReadOnlyList<MemberDetail> MemberDetails
        {
            get
            {
                if (memberDetails is null)
                {
                    lock (locker)
                    {
                        memberDetails ??= CreateMemberDetails();
                    }
                }
                return memberDetails;
            }
        }

        private Dictionary<string, MemberDetail>? membersByName = null;
        public override sealed MemberDetail GetMember(string name)
        {
            if (membersByName is null)
            {
                lock (locker)
                {
                    membersByName ??= this.MemberDetails.ToDictionary(x => x.Name);
                }
            }
            if (!this.membersByName.TryGetValue(name, out var member))
                throw new Exception($"{Type.Name} does not contain member {name}");
            return member;
        }
        public override sealed bool TryGetMember(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MemberDetail member)
        {
            if (membersByName is null)
            {
                lock (locker)
                {
                    membersByName ??= this.MemberDetails.ToDictionary(x => x.Name);
                }
            }
            return this.membersByName.TryGetValue(name, out member);
        }

        protected abstract Func<Attribute[]> CreateAttributes { get; }
        private Attribute[]? attributes = null;
        public override sealed IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (attributes is null)
                {
                    lock (locker)
                    {
                        attributes ??= CreateAttributes();
                    }
                }
                return attributes;
            }
        }

        public override sealed Delegate? CreatorTyped => throw new NotSupportedException();
        public override sealed Func<object> CreatorBoxed => throw new NotSupportedException();
        public override sealed bool HasCreatorBoxed => false;

        protected void LoadConstructorInfo()
        {
            if (Type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"ConstructorInfo cannot be used for unresolved generic types");

            var constructors = Type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var constructorParameters = constructors.ToDictionary(x => x, x => x.GetParameters());
            foreach (var constructorDetail in ConstructorDetailsBoxed)
            {
                var constructor = constructors.FirstOrDefault(x => SignatureCompareForLoadConstructorInfo(constructorParameters[x], constructorDetail));
                if (constructor is null)
                    throw new InvalidOperationException($"ConstructorInfo not found for generated constructor new({String.Join(", ", constructorDetail.ParameterDetails.Select(x => x.Type.GetNiceName()))})");

                var constructorBase = (ConstructorDetail)constructorDetail;
                constructorBase.SetConstructorInfo(constructor);
            }
        }

        protected void LoadMethodInfo()
        {
            if (Type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"MethodInfo cannot be used for unresolved generic types");

            var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var methodParameters = methods.ToDictionary(x => x, x => x.GetParameters());
            foreach (var methodDetail in MethodDetailsBoxed)
            {
                var method = methods.FirstOrDefault(x => SignatureCompareForLoadMethodInfo(x.Name, methodParameters[x], methodDetail));
                if (method is null)
                    throw new InvalidOperationException($"MethodInfo not found for generated method {methodDetail.Name}({String.Join(", ", methodDetail.ParameterDetails.Select(x => x.Type.GetNiceName()))})");

                var methodBase = (MethodDetail)methodDetail;
                methodBase.SetMethodInfo(method);
            }
        }

        protected void LoadMemberInfo()
        {
            if (Type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"MemberInfo cannot be used for unresolved generic types");

            IEnumerable<PropertyInfo> properties = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            IEnumerable<FieldInfo> fields = Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var memberDetail in MemberDetails)
            {
                var property = properties.FirstOrDefault(x => x.Name == memberDetail.Name);
                if (property is not null)
                {
                    //<{property.Name}>k__BackingField
                    //<{property.Name}>i__Field
                    var backingName = $"<{property.Name}>";
                    var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));

                    memberDetail.SetMemberInfo(property, backingField);
                    continue;
                }
                var field = fields.FirstOrDefault(x => x.Name == memberDetail.Name);
                if (field is not null)
                {
                    memberDetail.SetMemberInfo(field, null);
                    continue;
                }
                throw new InvalidOperationException($"MemberInfo not found for generated member {Type.GetNiceName()}.{memberDetail.Name}");
            }
        }
    }
}
