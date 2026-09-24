// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Reflection;
using Zerra.T4.CSharp;

namespace Zerra.T4.Test
{
    //The T4 parser reads source files from a directory, so each run writes its sources to a temp folder first
    public static class GeneratorRunner
    {
        public static string TypeScript(params string[] sources) => Run(ToFiles(sources), CQRSClientDomain.GenerateTypeScript);
        public static string JavaScript(params string[] sources) => Run(ToFiles(sources), CQRSClientDomain.GenerateJavaScript);
        public static CSharpSolution Parse(params string[] sources) => Run(ToFiles(sources), CSharpParser.ParseAllFiles);
        //paths are relative to the temp folder, for sources that need project files or folders around them
        public static CSharpSolution ParseFiles(params (string Path, string Text)[] files) => Run(files, CSharpParser.ParseAllFiles);

        private static (string, string)[] ToFiles(string[] sources) => sources.Select((x, i) => ($"Source{i}.cs", x)).ToArray();

        private static T Run<T>((string Path, string Text)[] files, Func<string, T> generate)
        {
            var directory = Path.Combine(Path.GetTempPath(), "Zerra.T4.Test", Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(directory);
            try
            {
                foreach (var file in files)
                {
                    var path = Path.Combine(directory, file.Path);
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, file.Text);
                }
                return generate(directory);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static string? solutionDirectory = null;
        public static string SolutionDirectory
        {
            get
            {
                if (solutionDirectory is null)
                {
                    var directory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
                    while (!File.Exists(Path.Combine(directory.FullName, "Zerra.slnx")) && directory.Parent is not null)
                        directory = directory.Parent;
                    solutionDirectory = directory.FullName;
                }
                return solutionDirectory;
            }
        }
    }
}
