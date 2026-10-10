// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Reflection;
using System.Runtime.Versioning;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.NetStandard
{
    public class NetStandardBuildTests
    {
        //.NET Framework gets the .NET Standard build, the code these tests are here for
        [Fact]
        public void RunsTheNetStandardBuild()
        {
            var framework = typeof(SymmetricEncryptor).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName;
            Assert.StartsWith(".NETStandard", framework);
        }
    }
}
