// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Metadata for a type method including its parameters and invocation delegates.
    /// Provides both unboxed and boxed delegates for efficient method invocation.
    /// Used by the source generator and runtime reflection to enable method calls.
    /// </summary>
    public partial class MethodDetail
    {
        protected readonly Lock locker = new();

        /// <summary>The parent type that owns this method.</summary>
        public readonly Type ParentType;
        /// <summary>The name of the method.</summary>
        public readonly string Name;
        /// <summary>The return type of the method.</summary>
        public readonly Type ReturnType;
        /// <summary>The number of generic argument for this method.</summary>
        public readonly int GenericArgumentCount;
        /// <summary>Collection of parameters required by this method.</summary>
        public readonly IReadOnlyList<ParameterDetail> Parameters;
        /// <summary>Indicates whether this method has a callable delegate.</summary>
        public readonly bool HasCaller;
        /// <summary>The unboxed delegate for invoking this method.</summary>
        public readonly Delegate? Caller;
        /// <summary>Indicates whether this method has a boxed callable delegate.</summary>
        public readonly bool HasCallerBoxed;
        /// <summary>Boxed delegate for invoking this method; accepts instance and parameter values as object array.</summary>
        public readonly Func<object?, object?[]?, object?>? CallerBoxed;
        /// <summary>Collection of all custom attributes applied to this method.</summary>
        public readonly IReadOnlyList<Attribute> Attributes;
        /// <summary>Indicates whether this method is static.</summary>
        public readonly bool IsStatic;
        /// <summary>Indicates whether this method is an explicit interface implementation.</summary>
        public readonly bool IsExplicitFromInterface;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodDetail"/> class with method metadata.
        /// </summary>
        /// <param name="parentType">The parent type that owns this method.</param>
        /// <param name="name">The name of the method.</param>
        /// <param name="returnType">The return type of the method.</param>
        /// <param name="genericArgumentCount">The number of generic arguments for this method.</param>
        /// <param name="parameters">The parameters required by this method.</param>
        /// <param name="caller">The unboxed delegate for invoking this method.</param>
        /// <param name="callerBoxed">Boxed delegate for invoking this method.</param>
        /// <param name="attributes">Custom attributes applied to the method.</param>
        /// <param name="isStatic">Whether this method is static.</param>
        /// <param name="isExplicitFromInterface">Whether this method is an explicit interface implementation.</param>
        public MethodDetail(Type parentType, string name, Type returnType, int genericArgumentCount, IReadOnlyList<ParameterDetail> parameters, Delegate? caller, Func<object?, object?[]?, object?>? callerBoxed, IReadOnlyList<Attribute> attributes, bool isStatic, bool isExplicitFromInterface)
        {
            this.ParentType = parentType;
            this.Name = name;
            this.ReturnType = returnType;
            this.GenericArgumentCount = genericArgumentCount;
            this.Parameters = parameters;
            this.HasCaller = caller != null;
            this.Caller = caller;
            this.HasCallerBoxed = callerBoxed != null;
            this.CallerBoxed = callerBoxed;
            this.Attributes = attributes;
            this.IsStatic = isStatic;
            this.IsExplicitFromInterface = isExplicitFromInterface;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Name}({String.Join(",", Parameters.Select(x => x.Type.Name))})";
        }
    }
}
