// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection.Dynamic
{
    /// <summary>
    /// Provides dependency resolution capabilities for retrieving interface implementations.
    /// Supports both singleton and factory patterns for creating instances of interface implementations discovered through reflection.
    /// </summary>
    /// <remarks>
    /// This resolver relies on the <see cref="Discovery"/> class to find interface implementations.
    /// Ensure <see cref="Discovery.Initialize(bool)"/> has been called before using any resolution methods.
    /// </remarks>
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    public static class Resolver
    {
        /// <summary>
        /// Attempts to retrieve a singleton instance of a class implementing the specified interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to resolve.</typeparam>
        /// <param name="provider">When the method returns true, contains the singleton instance; otherwise the default value.</param>
        /// <returns>True if an implementation was found and instantiated; otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when TInterface is not an interface.</exception>
        public static bool TryGetSingle<TInterface>(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)] out TInterface provider
#else
            out TInterface? provider
#endif
        )
        {
            provider = GetSingle<TInterface>(false);
            return provider is not null;
        }

        /// <summary>
        /// Retrieves a singleton instance of a class implementing the specified interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to resolve.</typeparam>
        /// <returns>The singleton instance of the interface implementation.</returns>
        /// <exception cref="ArgumentException">Thrown when TInterface is not an interface or no implementation is found.</exception>
        public static TInterface GetSingle<TInterface>() => GetSingle<TInterface>(true)!;

        /// <summary>
        /// Attempts to retrieve a singleton instance of a class implementing the specified interface type.
        /// </summary>
        /// <param name="type">The interface type to resolve.</param>
        /// <param name="provider">When the method returns true, contains the singleton instance; otherwise null.</param>
        /// <returns>True if an implementation was found and instantiated; otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not an interface.</exception>
        public static bool TryGetSingle(Type type, [MaybeNullWhen(false)] out object provider)
        {
            provider = GetSingle(type, false);
            return provider is not null;
        }

        /// <summary>
        /// Retrieves a singleton instance of a class implementing the specified interface type.
        /// </summary>
        /// <param name="type">The interface type to resolve.</param>
        /// <returns>The singleton instance of the interface implementation.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not an interface or no implementation is found.</exception>
        public static object GetSingle(Type type) => GetSingle(type, true)!;

        /// <summary>
        /// Attempts to create a new instance of a class implementing the specified interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to resolve.</typeparam>
        /// <param name="provider">When the method returns true, contains the new instance; otherwise the default value.</param>
        /// <returns>True if an implementation was found and instantiated; otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when TInterface is not an interface.</exception>
        public static bool TryGetNew<TInterface>(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)] out TInterface provider
#else
            out TInterface? provider
#endif
        )
        {
            provider = GetNew<TInterface>(false);
            return provider is not null;
        }

        /// <summary>
        /// Creates a new instance of a class implementing the specified interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to resolve.</typeparam>
        /// <returns>A new instance of the interface implementation.</returns>
        /// <exception cref="ArgumentException">Thrown when TInterface is not an interface or no implementation is found.</exception>
        public static TInterface GetNew<TInterface>() => GetNew<TInterface>(true)!;

        /// <summary>
        /// Attempts to create a new instance of a class implementing the specified interface type.
        /// </summary>
        /// <param name="type">The interface type to resolve.</param>
        /// <param name="provider">When the method returns true, contains the new instance; otherwise null.</param>
        /// <returns>True if an implementation was found and instantiated; otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not an interface.</exception>
        public static bool TryGetNew(Type type, [MaybeNullWhen(false)] out object provider)
        {
            provider = GetNew(type, false);
            return provider is not null;
        }

        /// <summary>
        /// Creates a new instance of a class implementing the specified interface type.
        /// </summary>
        /// <param name="type">The interface type to resolve.</param>
        /// <returns>A new instance of the interface implementation.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not an interface or no implementation is found.</exception>
        public static object GetNew(Type type) => GetNew(type, true)!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InterfaceT? GetSingle<InterfaceT>(bool throwException)
        {
            var interfaceType = typeof(InterfaceT);
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, throwException);

            if (providerType is null)
                return default;

            var provider = (InterfaceT)Instantiator.GetSingle(providerType);
            if (provider is null)
                return provider;

            return provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? GetSingle(Type interfaceType, bool throwException)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, throwException);

            if (providerType is null)
                return default;

            var provider = Instantiator.GetSingle(providerType);
            if (provider is null)
                return provider;

            return provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InterfaceT? GetNew<InterfaceT>(bool throwException)
        {
            var interfaceType = typeof(InterfaceT);
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, throwException);

            if (providerType is null)
                return default;

            var provider = (InterfaceT)Instantiator.Create(providerType);
            if (provider is null)
                return provider;

            return provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? GetNew(Type interfaceType, bool throwException)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, throwException);

            if (providerType is null)
                return default;

            var provider = Instantiator.Create(providerType);
            if (provider is null)
                return provider;

            return provider;
        }
    }
}
