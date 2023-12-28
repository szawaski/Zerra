// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Accessor Optimizations")]
[assembly: SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>")]
[assembly: SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "<Pending>")]
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "NetStandard2.0 does not support")]
[assembly: SuppressMessage("Style", "IDE0056:Use index operator", Justification = "NetStandard2.0 does not support")]
[assembly: SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException throw helper", Justification = "Only net8.0 Support")]
[assembly: SuppressMessage("Style", "IDE0031:Use null propagation", Justification = "<Pending>")]
[assembly: SuppressMessage("Style", "IDE0270:Use coalesce expression", Justification = "<Pending>")]
