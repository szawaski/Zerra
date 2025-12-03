//// Copyright © KaKush LLC
//// Written By Steven Zawaski
//// Licensed to you under the MIT license

//namespace Zerra.SourceGeneration.Types
//{
//    /// <summary>
//    /// Generic version of method metadata with strongly-typed delegate for improved type safety.
//    /// </summary>
//    /// <typeparam name="T">The type that declares this method.</typeparam>
//    /// <typeparam name="V">The return type of this method.</typeparam>
//    public sealed class MethodDetail<V> : MethodDetail
//    {
//        /// <summary>The strongly-typed delegate for invoking this method.</summary>
//        public readonly new Func<object?, object?[]?, V> Caller;

//        /// <summary>
//        /// Initializes a new instance of the <see cref="MethodDetail{V}"/> class with strongly-typed method metadata.
//        /// </summary>
//        /// <param name="name">The name of the method.</param>
//        /// <param name="parameters">The parameters required by this method.</param>
//        /// <param name="caller">The strongly-typed delegate for invoking this method.</param>
//        /// <param name="callerBoxed">Boxed delegate for invoking this method.</param>
//        /// <param name="attributes">Custom attributes applied to the method.</param>
//        /// <param name="isStatic">Whether this method is static.</param>
//        /// <param name="isExplicitFromInterface">Whether this method is an explicit interface implementation.</param>
//        public MethodDetail(string name, IReadOnlyList<ParameterDetail> parameters, Func<object?, object?[]?, V> caller, Func<object?, object?[]?, object?> callerBoxed, IReadOnlyList<Attribute> attributes, bool isStatic, bool isExplicitFromInterface)
//            : base(name, parameters, caller, callerBoxed, attributes, isStatic, isExplicitFromInterface)
//        {
//            this.Caller = caller;
//        }
//    }
//}
