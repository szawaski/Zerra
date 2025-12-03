////// Copyright © KaKush LLC
////// Written By Steven Zawaski
////// Licensed to you under the MIT license

////namespace Zerra.SourceGeneration.Types
////{
////    /// <summary>
////    /// Metadata for a type method including its parameters and invocation delegates.
////    /// Provides both unboxed and boxed delegates for efficient method invocation.
////    /// Used by the source generator and runtime reflection to enable method calls.
////    /// </summary>
////    public class MethodDetail
////    {
////        /// <summary>The name of the method.</summary>
////        public readonly string Name;
////        /// <summary>Collection of parameters required by this method.</summary>
////        public readonly IReadOnlyList<ParameterDetail> Parameters;
////        /// <summary>The unboxed delegate for invoking this method.</summary>
////        public readonly Delegate Caller;
////        /// <summary>Boxed delegate for invoking this method; accepts instance and parameter values as object array.</summary>
////        public readonly Func<object, object?[]?, object?> CallerBoxed;
////        /// <summary>Collection of all custom attributes applied to this method.</summary>
////        public readonly IReadOnlyList<Attribute> Attributes;
////        /// <summary>Indicates whether this method is static.</summary>
////        public readonly bool IsStatic;
////        /// <summary>Indicates whether this method is an explicit interface implementation.</summary>
////        public readonly bool IsExplicitFromInterface;

////        /// <summary>
////        /// Initializes a new instance of the <see cref="MethodDetail"/> class with method metadata.
////        /// </summary>
////        /// <param name="name">The name of the method.</param>
////        /// <param name="parameters">The parameters required by this method.</param>
////        /// <param name="caller">The unboxed delegate for invoking this method.</param>
////        /// <param name="callerBoxed">Boxed delegate for invoking this method.</param>
////        /// <param name="attributes">Custom attributes applied to the method.</param>
////        /// <param name="isStatic">Whether this method is static.</param>
////        /// <param name="isExplicitFromInterface">Whether this method is an explicit interface implementation.</param>
////        public MethodDetail(string name, IReadOnlyList<ParameterDetail> parameters, Delegate caller, Func<object, object?[]?, object?> callerBoxed, IReadOnlyList<Attribute> attributes, bool isStatic, bool isExplicitFromInterface)
////        {
////            this.Name = name;
////            this.Parameters = parameters;
////            this.Caller = caller;
////            this.CallerBoxed = callerBoxed;
////            this.Attributes = attributes;
////            this.IsStatic = isStatic;
////            this.IsExplicitFromInterface = isExplicitFromInterface;
////        }
////    }
////}
