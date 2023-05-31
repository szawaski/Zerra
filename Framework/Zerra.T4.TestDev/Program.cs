﻿using System;
using System.IO;

namespace Zerra.T4.TestDev
{
    class Program
    {
        static void Main(string[] args)
        {
            var directory = Environment.CurrentDirectory;
            while (Directory.GetFiles(directory, "*.sln").Length == 0)
                directory = new DirectoryInfo(directory).Parent.FullName;

            directory = @"C:\Users\Steven Zawaski\Documents\GitHub\Arcoro.Integrations-V2";

            var ts = CQRSClientDomain.GenerateTypeScript(directory);
            var js = CQRSClientDomain.GenerateJavaScript(directory);
        }
    }
}
