using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection.Generation
{
    public abstract class TypeDetailTGenerationBase<T> : TypeDetail<T>
    {
        public TypeDetailTGenerationBase() : base(typeof(T)) { }

        private TypeDetail[]? innerTypesDetails = null;
        public override sealed IReadOnlyList<TypeDetail> InnerTypeDetails
        {
            get
            {
                if (innerTypesDetails == null)
                {
                    lock (locker)
                    {
                        if (innerTypesDetails == null)
                        {
                            var innerTypesRef = InnerTypes;
                            if (innerTypesRef != null)
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
                if (IEnumerableGenericInnerType == null)
                    throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not an IEnumerableGeneric");
                if (iEnumerableGenericInnerTypeDetail == null)
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

        protected abstract Func<MethodDetail<T>[]> CreateMethodDetails { get; }
        private MethodDetail<T>[]? methodDetails = null;
        public override sealed IReadOnlyList<MethodDetail<T>> MethodDetails
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

        protected abstract Func<ConstructorDetail<T>[]> CreateConstructorDetails { get; }
        private ConstructorDetail<T>[]? constructorDetails = null;
        public override sealed IReadOnlyList<ConstructorDetail<T>> ConstructorDetails
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

        protected void LoadConstructorInfo()
        {
            if (Type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"ConstructorInfo cannot be used for unresolved generic types");

            var constructors = Type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var constructorParameters = constructors.ToDictionary(x => x, x => x.GetParameters());
            foreach (var constructorDetail in ConstructorDetails)
            {
                var constructor = constructors.FirstOrDefault(x => SignatureCompare(constructorParameters[x], constructorDetail));
                if (constructor == null)
                    throw new InvalidOperationException($"ConstructorInfo not found for generated constructor new({String.Join(", ", constructorDetail.ParametersDetails.Select(x => x.Type.GetNiceName()))})");

                var constructorBase = (ConstructorDetailGenerationBase<T>)constructorDetail;
                constructorBase.SetConstructorInfo(constructor);
            }
        }

        protected void LoadMethodInfo()
        {
            if (Type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"MethodInfo cannot be used for unresolved generic types");

            var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var methodParameters = methods.ToDictionary(x => x, x => x.GetParameters());
            foreach (var methodDetail in MethodDetails)
            {
                var methodBase = (MethodDetailGenerationBase<T>)methodDetail;
                var method = methods.FirstOrDefault(x => SignatureCompare(x.Name, methodParameters[x], methodDetail));
                if (method == null)
                    throw new InvalidOperationException($"MethodInfo not found for generated method {methodDetail.Name}({String.Join(", ", methodDetail.ParameterDetails.Select(x => x.Type.GetNiceName()))})");

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
                if (property != null)
                {
                    memberDetail.SetMemberInfo(property);
                    continue;
                }
                var field = fields.FirstOrDefault(x => x.Name == memberDetail.Name);
                if (field != null)
                {
                    memberDetail.SetMemberInfo(field);
                    continue;
                }
                throw new InvalidOperationException($"MemberInfo not found for generated member {Type.GetNiceName()}.{memberDetail.Name}");
            }
        }
    }
}
