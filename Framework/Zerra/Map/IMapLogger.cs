// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    /// <summary>
    /// Defines a logger that will record the mappings that occured with passed into a <seealso cref="Mapper"/> method.
    /// </summary>
    public interface IMapLogger
    {
        /// <summary>
        /// Log a target propery that was changed.
        /// </summary>
        /// <param name="source">The source property as a string representation.</param>
        /// <param name="sourceValue">The value from the source that will be applied to the target property.</param>
        /// <param name="target">The target property as a string representation</param>
        /// <param name="targetValue">The original value of the target property.</param>
        void LogPropertyChange(string source, string sourceValue, string target, string targetValue);
        /// <summary>
        /// Logs that a targert property was not changed.
        /// </summary>
        /// <param name="source">The source property as a string representation.</param>
        /// <param name="target">The target property as a string representation</param>
        /// <param name="value">The value of the target property that will not change.</param>
        void LogPropertyNoChange(string source, string target, string value);
        /// <summary>
        /// Logs that a new object was created for a target property.
        /// </summary>
        /// <param name="source">The source property as a string representation.</param>
        /// <param name="target">The target property as a string representation</param>
        /// <param name="type">The object type as a string representation.</param>
        void LogNewObject(string source, string target, string type);
    }
}
