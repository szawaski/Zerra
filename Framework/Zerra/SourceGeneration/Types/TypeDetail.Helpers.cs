// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Zerra.Collections;
using Zerra.SourceGeneration.Reflection;

namespace Zerra.SourceGeneration.Types
{
    public partial class TypeDetail
    {
        private TypeDetail? innerTypeDetail = null;
        /// <summary>Gets the type detail for <see cref="InnerType"/> if this is a nullable type; otherwise null.</summary>
        public TypeDetail? InnerTypeDetail
        {
            get
            {
                if (innerTypeDetail == null && InnerType != null)
                    innerTypeDetail = TypeAnalyzer.GetTypeDetail(InnerType);
                return innerTypeDetail;
            }
        }

        private TypeDetail? iEnumerableGenericInnerTypeDetail = null;
        /// <summary>Gets the type detail for <see cref="IEnumerableGenericInnerType"/> if this type is enumerable; otherwise null.</summary>
        public TypeDetail? IEnumerableGenericInnerTypeDetail
        {
            get
            {
                if (iEnumerableGenericInnerTypeDetail == null && IEnumerableGenericInnerType != null)
                    iEnumerableGenericInnerTypeDetail = TypeAnalyzer.GetTypeDetail(IEnumerableGenericInnerType);
                return iEnumerableGenericInnerTypeDetail;
            }
        }

        private TypeDetail? dictionaryInnerTypeDetail = null;
        /// <summary>Gets the type detail for <see cref="DictionaryInnerType"/> if this type is a dictionary; otherwise null.</summary>
        public TypeDetail? DictionaryInnerTypeDetail
        {
            get
            {
                if (dictionaryInnerTypeDetail == null && DictionaryInnerType != null)
                    dictionaryInnerTypeDetail = TypeAnalyzer.GetTypeDetail(DictionaryInnerType);
                return dictionaryInnerTypeDetail;
            }
        }

        private IReadOnlyList<TypeDetail>? innerTypeDetails = null;
        /// <summary>Gets the type detail for <see cref="InnerTypes"/> for generic types; empty if not generic.</summary>
        public IReadOnlyList<TypeDetail> InnerTypeDetails
        {
            get
            {
                innerTypeDetails ??= InnerTypes.Select(TypeAnalyzer.GetTypeDetail).ToArray();
                return innerTypeDetails;
            }
        }

        private IReadOnlyList<MemberDetail>? serializableMemberDetails = null;
        /// <summary>
        /// Gets a cached collection of members that can be serialized.
        /// Includes non-static, backed members that are not explicit interface implementations and have serializable types.
        /// </summary>
        public IReadOnlyList<MemberDetail> SerializableMemberDetails
        {
            get
            {
                serializableMemberDetails ??= Members.Where(x => !x.IsStatic && x.IsBacked && !x.IsExplicitFromInterface && IsSerializableType(x.TypeDetail)).ToArray();
                return serializableMemberDetails;
            }
        }

        private Dictionary<string, MemberDetail>? membersByName = null;
        /// <summary>
        /// Attempts to retrieve a member by its name.
        /// </summary>
        /// <param name="name">The member name to look up.</param>
        /// <param name="member">When the method returns true, contains the <see cref="MemberDetail"/> associated with the specified name; otherwise the default value.</param>
        /// <returns>True if a member with the specified name was found; otherwise false.</returns>
        public bool TryGetMember(string name,
#if !NETSTANDARD2_0
           [MaybeNullWhen(false)]
#endif
        out MemberDetail member)
        {
            membersByName ??= Members.ToDictionary(x => x.Name);

            if (membersByName.TryGetValue(name, out member))
                return true;

            return false;
        }

        public MemberDetail GetMember(string name)
        {
            if (TryGetMember(name, out var member))
                return member;
            throw new ArgumentException($"No member named {name} found on type {Type.Name}");
        }

        private Dictionary<string, MethodDetail[]>? methodsByName = null;
        /// <summary>
        /// Attempts to retrieve a method by its name.
        /// </summary>
        /// <param name="name">The method name to look up.</param>
        /// <param name="method">When the method returns true, contains the first <see cref="MethodDetail"/> found with the specified name; otherwise the default value.</param>
        /// <returns>True if a method with the specified name was found; otherwise false.</returns>
        public bool TryGetMethod(string name,
#if !NETSTANDARD2_0
           [MaybeNullWhen(false)]
#endif
        out MethodDetail method)
        {
            methodsByName ??= Methods.GroupBy(x => x.Name).ToDictionary(g => g.Key, g => g.ToArray());

            if (methodsByName.TryGetValue(name, out var methodArray))
            {
                method = methodArray.FirstOrDefault();
                return method != null;
            }

            method = null;
            return false;
        }

        private ConcurrentFactoryDictionary<TypeKey, MethodDetail?>? methodLookups = null;
        private MethodDetail? GetMethodInternal(string name, int? parameterCount, Type[]? parameterTypes)
        {
            if (parameterCount is not null && parameterTypes is not null && parameterTypes.Length != parameterCount)
                throw new InvalidOperationException($"Number of parameters does not match the specified count");

            var key = new TypeKey(name, parameterCount, parameterTypes);
            methodLookups ??= new();
            var method = methodLookups.GetOrAdd(key, Methods, name, parameterCount, parameterTypes, static (Methods, name, parameterCount, parameterTypes) =>
            {
                MethodDetail? found = null;
                foreach (var methodDetail in Methods)
                {
                    if (SignatureCompare(name, parameterCount, parameterTypes, methodDetail))
                    {
                        if (found is not null)
                            throw new InvalidOperationException($"More than one method found for {name}");
                        found = methodDetail;
                    }
                }
                return found;
            });
            return method;
        }
        /// <summary>
        /// Retrieves a method by its name.
        /// </summary>
        /// <param name="name">The method name to look up.</param>
        /// <returns>The <see cref="MethodDetail"/> for the specified method name.</returns>
        /// <exception cref="MissingMethodException">Thrown when no method with the specified name is found.</exception>
        public MethodDetail GetMethod(string name)
        {
            var method = GetMethodInternal(name, null, null);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        /// <summary>
        /// Retrieves a method by its name and parameter count.
        /// </summary>
        /// <param name="name">The method name to look up.</param>
        /// <param name="parameterCount">The number of parameters the method should have.</param>
        /// <returns>The <see cref="MethodDetail"/> for the specified method name and parameter count.</returns>
        /// <exception cref="MissingMethodException">Thrown when no method matching the criteria is found.</exception>
        public MethodDetail GetMethod(string name, int parameterCount)
        {
            var method = GetMethodInternal(name, parameterCount, null);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        /// <summary>
        /// Retrieves a method by its name and parameter types.
        /// </summary>
        /// <param name="name">The method name to look up.</param>
        /// <param name="parameterTypes">The types of parameters the method should have.</param>
        /// <returns>The <see cref="MethodDetail"/> for the specified method name and parameter types.</returns>
        /// <exception cref="MissingMethodException">Thrown when no method matching the criteria is found.</exception>
        public MethodDetail GetMethod(string name, Type[] parameterTypes)
        {
            var method = GetMethodInternal(name, null, parameterTypes);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }

        private ConcurrentFactoryDictionary<TypeKey, ConstructorDetail?>? constructorLookups = null;
        private ConstructorDetail? GetConstructorInternal(int? parameterCount, Type[]? parameterTypes)
        {
            var key = new TypeKey(parameterTypes);
            constructorLookups ??= new();
            var constructor = constructorLookups.GetOrAdd(key, Constructors, parameterCount, parameterTypes, static (Constructors, parameterCount, parameterTypes) =>
            {
                ConstructorDetail? found = null;
                foreach (var constructorDetail in Constructors)
                {
                    if (SignatureCompare(parameterCount, parameterTypes, constructorDetail))
                    {
                        if (found is not null)
                            throw new InvalidOperationException($"More than one constructor found");
                        found = constructorDetail;
                    }
                }
                return found;
            });
            return constructor;
        }
        /// <summary>
        /// Retrieves the parameterless constructor.
        /// </summary>
        /// <returns>The <see cref="ConstructorDetail"/> for the parameterless constructor.</returns>
        /// <exception cref="MissingMethodException">Thrown when no parameterless constructor is found.</exception>
        public ConstructorDetail GetConstructor()
        {
            var constructor = GetConstructorInternal(null, null);
            if (constructor is null)
                throw new MissingMethodException($"{Type.Name} constructor not found");
            return constructor;
        }
        /// <summary>
        /// Retrieves a constructor by its parameter count.
        /// </summary>
        /// <param name="parameterCount">The number of parameters the constructor should have.</param>
        /// <returns>The <see cref="ConstructorDetail"/> for the specified parameter count.</returns>
        /// <exception cref="MissingMethodException">Thrown when no constructor matching the criteria is found.</exception>
        public ConstructorDetail GetConstructor(int parameterCount)
        {
            var constructor = GetConstructorInternal(parameterCount, null);
            if (constructor is null)
                throw new MissingMethodException($"{Type.Name} constructor not found");
            return constructor;
        }
        /// <summary>
        /// Retrieves a constructor by its parameter types.
        /// </summary>
        /// <param name="parameterTypes">The types of parameters the constructor should have.</param>
        /// <returns>The <see cref="ConstructorDetail"/> for the specified parameter types.</returns>
        /// <exception cref="MissingMethodException">Thrown when no constructor matching the criteria is found.</exception>
        public ConstructorDetail GetConstructor(Type[] parameterTypes)
        {
            var constructor = GetConstructorInternal(null, parameterTypes);
            if (constructor is null)
                throw new MissingMethodException($"{Type.Name} constructor not found");
            return constructor;
        }

        private static bool IsSerializableType(TypeDetail typeDetail)
        {
            if (typeDetail.CoreType.HasValue)
                return true;
            if (typeDetail.Type.IsEnum)
                return true;
            if (typeDetail.Type.IsArray)
                return true;
            if (typeDetail.IsNullable)
                return IsSerializableType(typeDetail.InnerTypeDetail!);
            if (typeDetail.Type.IsClass)
                return true;
            if (typeDetail.Type.IsInterface)
                return true;
            return false;
        }

        protected static bool SignatureCompare(string name1, int? parameterCount, Type[]? parameters1, MethodDetail methodDetail2)
        {
            if (name1 != methodDetail2.Name)
                return false;

            if (parameterCount is not null)
            {
                if (parameterCount != methodDetail2.Parameters.Count)
                    return false;
            }

            if (parameters1 is not null)
            {
                if (parameters1.Length != methodDetail2.Parameters.Count)
                    return false;
                for (var i = 0; i < parameters1.Length; i++)
                {
                    var type1 = parameters1[i];
                    var type2 = methodDetail2.Parameters[i].Type;
                    if (type1 != type2)
                        return false;
                }
            }

            return true;
        }
        protected static bool SignatureCompare(int? parameterCount, Type[]? parameters1, ConstructorDetail constructorDetail2)
        {
            if (parameterCount is not null)
            {
                if (parameterCount != constructorDetail2.Parameters.Count)
                    return false;
            }

            if (parameters1 is not null)
            {
                if (parameters1.Length != constructorDetail2.Parameters.Count)
                    return false;
                for (var i = 0; i < parameters1.Length; i++)
                {
                    if (parameters1[i] != constructorDetail2.Parameters[i].Type)
                        return false;
                }
            }

            return true;
        }
    }
}
