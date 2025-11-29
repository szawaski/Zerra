// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Zerra.SourceGeneration.Test
{
    public static class DirectoryHelper
    {
        private static readonly object locker = new();

        private static DirectoryInfo? solutionDirectory = null;
        public static DirectoryInfo SolutionDirectory
        {
            get
            {
                if (solutionDirectory is null)
                {
                    lock (locker)
                    {
                        if (solutionDirectory is null)
                        {
                            var directory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
                            while (!File.Exists($"{directory.FullName}{Path.DirectorySeparatorChar}Zerra.slnx") && directory.Parent is not null)
                                directory = directory.Parent;
                            solutionDirectory = directory;
                        }
                    }
                }
                return solutionDirectory;
            }
        }

        private static DirectoryInfo? netCoreDirectory = null;
        public static DirectoryInfo NetCoreDirectory
        {
            get
            {
                if (netCoreDirectory is null)
                {
                    lock (locker)
                    {
                        if (netCoreDirectory is null)
                        {
                            var thisRuntimeAssembly = AppDomain.CurrentDomain.GetAssemblies().First(x => x.Location.EndsWith("System.Runtime.dll"));
                            var netCorePath = new DirectoryInfo(thisRuntimeAssembly.Location);
                            netCoreDirectory = netCorePath.Parent!.Parent!;
                        }
                    }
                }
                return netCoreDirectory;
            }
        }
    }
}
