using System.IO;

namespace Zerra.Test
{
    public static class TypesToGenerateCode
    {
        private static string? code = null;
        public static string Code
        {
            get
            {
                if (code is null)
                {
                    var directory = DirectoryHelper.SolutionDirectory;
                    var filePath = $"{directory.FullName}{Path.DirectorySeparatorChar}Framework{Path.DirectorySeparatorChar}Zerra{Path.DirectorySeparatorChar}Reflection{Path.DirectorySeparatorChar}Compiletime{Path.DirectorySeparatorChar}TypesToGenerate.cs";
                    code = File.ReadAllText(filePath);                    
                }
                return code;
            }
        }
    }
}
