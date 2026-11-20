// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Zerra.Reflection.Dynamic;

namespace Zerra.Reflection
{
    /// <summary>
    /// Every member, method, and constructor of a type, read through reflection: private, internal, static, and explicit interface implementations,
    /// with accessors for each. A base type's private members are on the base type's own detail.
    /// Details the type's <see cref="TypeDetail"/> already has in full are the same instances.
    /// Get it from <see cref="TypeDetail.GetFullAccess"/>.
    /// </summary>
#if !NETSTANDARD2_0
    [RequiresUnreferencedCode("Full access reads private and non-public members through reflection, trimming can remove them")]
    [RequiresDynamicCode("Full access builds its accessors at runtime")]
#endif
    public class TypeDetailFullAccess
    {
        private const BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

#if NETSTANDARD2_0
        private protected readonly object locker = new();
#else
        private protected readonly Lock locker = new();
#endif

        private readonly TypeDetail typeDetail;

        internal TypeDetailFullAccess(TypeDetail typeDetail)
        {
            this.typeDetail = typeDetail;
        }

        /// <summary>The type these details describe.</summary>
        public Type Type => typeDetail.Type;

        private IReadOnlyList<MemberDetail>? members;
        /// <summary>Every property and field.</summary>
        public IReadOnlyList<MemberDetail> Members
        {
            get
            {
                if (members == null)
                {
                    lock (locker)
                        members ??= TypeDetailGenerator.GenerateFullAccessMembers(typeDetail.Type, typeDetail.Interfaces, typeDetail.Members);
                }
                return members;
            }
        }

        private IReadOnlyList<MethodDetail>? methods;
        /// <summary>Every method, including static, property and event accessors, operators, explicit interface implementations, and methods with ref or out parameters.</summary>
        public IReadOnlyList<MethodDetail> Methods
        {
            get
            {
                if (methods == null)
                {
                    lock (locker)
                        methods ??= TypeDetailGenerator.GenerateFullAccessMethods(typeDetail.Type, typeDetail.Interfaces, typeDetail.Methods);
                }
                return methods;
            }
        }

        private IReadOnlyList<ConstructorDetail>? constructors;
        /// <summary>Every instance constructor with its actual parameters.</summary>
        public IReadOnlyList<ConstructorDetail> Constructors
        {
            get
            {
                if (constructors == null)
                {
                    lock (locker)
                        constructors ??= TypeDetailGenerator.GenerateFullAccessConstructors(typeDetail.Type, typeDetail.Constructors);
                }
                return constructors;
            }
        }

        private Dictionary<string, MemberDetail>? membersByName;
        private Dictionary<string, MemberDetail> MembersByName
        {
            get
            {
                if (membersByName == null)
                {
                    lock (locker)
                        membersByName ??= Members.ToDictionary(x => x.Name);
                }
                return membersByName;
            }
        }

        private Dictionary<string, MethodDetail[]>? methodsByName;
        private Dictionary<string, MethodDetail[]> MethodsByName
        {
            get
            {
                if (methodsByName == null)
                {
                    lock (locker)
                        methodsByName ??= Methods.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToArray());
                }
                return methodsByName;
            }
        }

        /// <summary>
        /// Gets the member with this name.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <returns>The member.</returns>
        public MemberDetail GetMember(string name)
        {
            if (!MembersByName.TryGetValue(name, out var member))
                throw new MissingMemberException($"{Type.Name}.{name} not found");
            return member;
        }

        /// <summary>
        /// Attempts to get the member with this name.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <param name="member">The member when found.</param>
        /// <returns>True when found.</returns>
        public bool TryGetMember(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
            out MemberDetail member)
            => MembersByName.TryGetValue(name, out member);

        /// <summary>
        /// Gets every method with this name, including overloads.
        /// </summary>
        /// <param name="name">The method name.</param>
        /// <returns>The methods, empty when there are none.</returns>
        public IReadOnlyList<MethodDetail> GetMethods(string name) => MethodsByName.TryGetValue(name, out var found) ? found : [];

        /// <summary>
        /// Gets the reflection member a detail describes, for its accessibility, every attribute, and anything else reflection has.
        /// </summary>
        /// <param name="member">A member from <see cref="Members"/> or the type's <see cref="TypeDetail.Members"/>.</param>
        /// <returns>The <see cref="PropertyInfo"/> or <see cref="FieldInfo"/>.</returns>
        public MemberInfo GetMemberInfo(MemberDetail member)
        {
            var found = FindMemberInfo(member.ParentType, member);
            //an interface's members include those of the interfaces it extends
            if (found is null && member.ParentType.IsInterface)
            {
                foreach (var i in member.ParentType.GetInterfaces())
                {
                    found = FindMemberInfo(i, member);
                    if (found is not null)
                        break;
                }
            }
            return found ?? throw new MissingMemberException($"{member.ParentType.Name}.{member.Name} not found");
        }

        /// <summary>
        /// Gets the reflection method a detail describes, for its accessibility, every attribute, and anything else reflection has.
        /// </summary>
        /// <param name="method">A method from <see cref="Methods"/> or the type's <see cref="TypeDetail.Methods"/>.</param>
        /// <returns>The <see cref="MethodInfo"/>.</returns>
        public MethodInfo GetMethodInfo(MethodDetail method)
        {
            var found = FindMethodInfo(method.ParentType, method);
            //an interface's methods include those of the interfaces it extends
            if (found is null && method.ParentType.IsInterface)
            {
                foreach (var i in method.ParentType.GetInterfaces())
                {
                    found = FindMethodInfo(i, method);
                    if (found is not null)
                        break;
                }
            }
            return found ?? throw new MissingMethodException($"{method.ParentType.Name}.{method.Name} not found");
        }

        /// <summary>
        /// Gets the reflection constructor a detail describes. A constructor from the type's <see cref="TypeDetail.Constructors"/>
        /// that also takes its required members has no single matching constructor.
        /// </summary>
        /// <param name="constructor">A constructor from <see cref="Constructors"/> or the type's <see cref="TypeDetail.Constructors"/>.</param>
        /// <returns>The <see cref="ConstructorInfo"/>, or null when there isn't one with these parameters.</returns>
        public ConstructorInfo? GetConstructorInfo(ConstructorDetail constructor)
            => constructor.ParentType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, constructor.Parameters.Select(x => x.Type).ToArray(), null);

        private static MemberInfo? FindMemberInfo(Type type, MemberDetail member)
            => member.IsField
                ? type.GetField(member.Name, allFlags)
                : type.GetProperties(allFlags).FirstOrDefault(x => x.Name == member.Name && x.GetIndexParameters().Length == 0);

        private static MethodInfo? FindMethodInfo(Type type, MethodDetail method)
            => type.GetMethods(allFlags).FirstOrDefault(x => x.Name == method.Name && x.IsStatic == method.IsStatic && x.GetParameters().Select(p => p.ParameterType).SequenceEqual(method.Parameters.Select(p => p.Type)));
    }
}
