// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.Logger
{
    public static class LogFile
    {
        private static readonly SemaphoreSlim fileLock = new SemaphoreSlim(1, 1);
        public static async Task Log(string fileName, string category, string message)
        {
            if (!String.IsNullOrWhiteSpace(message))
            {
                await fileLock.WaitAsync();
                FileStream fileStream = null;
                try
                {
                    fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                    fileStream.Position = fileStream.Length;

                    await fileStream.WriteAsync(Environment.NewLine);
                    await fileStream.WriteAsync(DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));
                    if (!String.IsNullOrWhiteSpace(category))
                    {
                        await fileStream.WriteAsync(" ");
                        await fileStream.WriteAsync(category);
                    }
                    if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null)
                    {
                        await fileStream.WriteAsync(" - ");
                        await fileStream.WriteAsync(Thread.CurrentPrincipal.Identity?.Name);
                    }
                    await fileStream.WriteAsync(" - ");
                    await fileStream.WriteAsync(message);
                    await fileStream.FlushAsync();
                    fileStream.Close();
                }
                catch
                {
                    if (System.Diagnostics.Debugger.IsAttached)
                        System.Diagnostics.Debugger.Break();
                }
                finally
                {
                    fileStream.Dispose();
                    fileLock.Release();
                }
            }
        }

        private static Task WriteAsync(this FileStream fileStream, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return fileStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
