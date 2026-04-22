// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Specifies the category of a C# type declaration.
    /// </summary>
    public enum CSharpObjectType
    {
        /// <summary>
        /// A class type.
        /// </summary>
        Class,

        /// <summary>
        /// A struct type.
        /// </summary>
        Struct,

        /// <summary>
        /// An interface type.
        /// </summary>
        Interface,

        /// <summary>
        /// An enumeration type.
        /// </summary>
        Enum,

        /// <summary>
        /// A delegate type.
        /// </summary>
        Delegate,

        /// <summary>
        /// A record type.
        /// </summary>
        Record
    }
}