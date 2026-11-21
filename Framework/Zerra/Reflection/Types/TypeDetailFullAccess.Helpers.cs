// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public partial class TypeDetailFullAccess
    {
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
            if (membersByName == null)
            {
                lock (locker)
                    membersByName ??= Members.ToDictionary(x => x.Name);
            }

            if (membersByName.TryGetValue(name, out member))
                return true;

            return false;
        }

        /// <summary>
        /// Retrieves a member by its name.
        /// </summary>
        /// <param name="name">The member name to look up.</param>
        /// <returns>The <see cref="MemberDetail"/> for the specified member name.</returns>
        /// <exception cref="MissingMemberException">Thrown when no member with the specified name is found.</exception>
        public MemberDetail GetMember(string name)
        {
            if (TryGetMember(name, out var member))
                return member;
            throw new MissingMemberException($"{Type.Name}.{name} member not found");
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
            if (methodsByName == null)
            {
                lock (locker)
                    methodsByName ??= Methods.GroupBy(x => x.Name).ToDictionary(g => g.Key, g => g.ToArray());
            }

            if (methodsByName.TryGetValue(name, out var methodArray))
            {
                method = methodArray.FirstOrDefault();
                return method != null;
            }

            method = null;
            return false;
        }

        /// <summary>
        /// Retrieves every method with the specified name, including overloads.
        /// </summary>
        /// <param name="name">The method name to look up.</param>
        /// <returns>The methods with the specified name, empty when there are none.</returns>
        public IReadOnlyList<MethodDetail> GetMethods(string name)
        {
            if (methodsByName == null)
            {
                lock (locker)
                    methodsByName ??= Methods.GroupBy(x => x.Name).ToDictionary(g => g.Key, g => g.ToArray());
            }

            if (methodsByName.TryGetValue(name, out var methodArray))
                return methodArray;

            return [];
        }

        private ConcurrentFactoryDictionary<TypeKey, MethodDetail?>? methodLookups = null;
        private MethodDetail? GetMethodInternal(string name, int? genericArgumentCount, int? parameterCount, Type[]? parameterTypes)
        {
            if (parameterCount is not null && parameterTypes is not null && parameterTypes.Length != parameterCount)
                throw new InvalidOperationException($"Number of parameters does not match the specified count");

            var key = new TypeKey(name, genericArgumentCount, parameterCount, parameterTypes);
            methodLookups ??= new();
            var method = methodLookups.GetOrAdd(key, Methods, name, genericArgumentCount, parameterCount, parameterTypes, static (Methods, name, genericArgumentCount, parameterCount, parameterTypes) =>
            {
                MethodDetail? found = null;
                foreach (var methodDetail in Methods)
                {
                    if (!TypeDetail.SignatureCompare(name, genericArgumentCount, parameterCount, parameterTypes, methodDetail))
                        continue;

                    if (found is not null)
                        throw new InvalidOperationException($"More than one method found for {name}");
                    found = methodDetail;
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
            var method = GetMethodInternal(name, null, null, null);
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
            var method = GetMethodInternal(name, null, parameterCount, null);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        /// <summary>
        /// Retrieves a method by its name, generic argument count, and parameter count.
        /// </summary>
        /// <param name="name">The method name to look up.</param>
        /// <param name="genericArgumentCount">The number of generic arguments the method should have.</param>
        /// <param name="parameterCount">The number of parameters the method should have.</param>
        /// <returns>The <see cref="MethodDetail"/> for the specified method name, generic argument count, and parameter count.</returns>
        /// <exception cref="MissingMethodException">Thrown when no method matching the criteria is found.</exception>
        public MethodDetail GetMethod(string name, int genericArgumentCount, int parameterCount)
        {
            var method = GetMethodInternal(name, genericArgumentCount, parameterCount, null);
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
            var method = GetMethodInternal(name, null, null, parameterTypes);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        /// <summary>
        /// Retrieves a method by its name, generic argument count, and parameter types.
        /// </summary>
        /// <param name="name">The method name to look up.</param>
        /// <param name="genericArgumentCount">The number of generic arguments the method should have.</param>
        /// <param name="parameterTypes">The types of parameters the method should have.</param>
        /// <returns>The <see cref="MethodDetail"/> for the specified method name, generic argument count, and parameter types.</returns>
        /// <exception cref="MissingMethodException">Thrown when no method matching the criteria is found.</exception>
        public MethodDetail GetMethod(string name, int genericArgumentCount, Type[] parameterTypes)
        {
            var method = GetMethodInternal(name, genericArgumentCount, null, parameterTypes);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }

        private ConcurrentFactoryDictionary<TypeKey, ConstructorDetail?>? constructorLookups = null;
        private ConstructorDetail? GetConstructorInternal(int? parameterCount, Type[]? parameterTypes)
        {
            var key = new TypeKey(null, parameterCount, null, parameterTypes);
            constructorLookups ??= new();
            var constructor = constructorLookups.GetOrAdd(key, Constructors, parameterCount, parameterTypes, static (Constructors, parameterCount, parameterTypes) =>
            {
                ConstructorDetail? found = null;
                foreach (var constructorDetail in Constructors)
                {
                    if (TypeDetail.SignatureCompare(parameterCount, parameterTypes, constructorDetail))
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
            var constructor = GetConstructorInternal(0, null);
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
    }
}
