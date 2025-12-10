// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    /// <summary>
    /// Specifies that a property or field should be ignored during JSON serialization or deserialization based on the specified condition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonIgnoreAttribute : Attribute
    {
        /// <summary>
        /// Gets the condition that determines when the property or field should be ignored.
        /// </summary>
        public JsonIgnoreCondition Condition { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonIgnoreAttribute"/> class with the default condition of <see cref="JsonIgnoreCondition.Always"/>.
        /// </summary>
        public JsonIgnoreAttribute()
        {
            this.Condition = JsonIgnoreCondition.Always;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonIgnoreAttribute"/> class with the specified ignore condition.
        /// </summary>
        /// <param name="condition">The <see cref="JsonIgnoreCondition"/> that specifies when the property or field should be ignored.</param>
        public JsonIgnoreAttribute(JsonIgnoreCondition condition)
        {
            this.Condition = condition;
        }
    }
}