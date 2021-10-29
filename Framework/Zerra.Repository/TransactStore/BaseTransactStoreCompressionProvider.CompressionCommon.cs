// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseTransactStoreCompressionProvider<TNextProviderInterface, TModel> where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        private static class CompressionCommon
        {
            private const string compressionPrefix = "<c>";

            public static string CompressGZip(string plain)
            {
                if (String.IsNullOrWhiteSpace(plain))
                    return plain;

                if (plain.Length > compressionPrefix.Length && plain.Substring(0, compressionPrefix.Length) == compressionPrefix)
                {
                    return plain;
                }
                var plainBytes = Encoding.Unicode.GetBytes(plain);
                var compressedBytes = CompressGZip(plainBytes);
                var compressed = compressionPrefix + Convert.ToBase64String(compressedBytes);
                return compressed;
            }
            public static byte[] CompressGZip(byte[] plain)
            {
                if (plain == null)
                    return plain;

                using (var msIn = new MemoryStream(plain))
                using (var msOut = new MemoryStream())
                {
                    using (var gzipStream = CompressGZip(msOut))
                    {
                        msIn.CopyTo(gzipStream);
                    }
                    byte[] compressed = msOut.ToArray();
                    return compressed;
                }
            }
            public static GZipStream CompressGZip(Stream stream)
            {
                var gzipStream = new GZipStream(stream, CompressionLevel.Fastest);
                return gzipStream;
            }

            public static string DecompressGZip(string compressed)
            {
                if (String.IsNullOrWhiteSpace(compressed))
                    return compressed;

                string compressedWithoutPrefix;
                if (compressed.Length > compressionPrefix.Length && compressed.Substring(0, compressionPrefix.Length) == compressionPrefix)
                    compressedWithoutPrefix = compressed.Substring(compressionPrefix.Length);
                else
                    return compressed;

                var compressedBytes = Convert.FromBase64String(compressedWithoutPrefix);
                var plainBytes = DecompressGZip(compressedBytes);
                string plain = Encoding.Unicode.GetString(plainBytes);
                return plain;
            }
            public static byte[] DecompressGZip(byte[] compressed)
            {
                if (compressed == null)
                    return compressed;

                using (var msIn = new MemoryStream(compressed))
                using (var msOut = new MemoryStream())
                {
                    using (var gzipStream = DecompressGZip(msIn))
                    {
                        gzipStream.CopyTo(msOut);
                    }
                    byte[] plain = msOut.ToArray();
                    return plain;
                }
            }
            public static GZipStream DecompressGZip(Stream stream)
            {
                var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                return gzipStream;
            }

            private static readonly ConcurrentFactoryDictionary<TypeKey, MemberDetail[]> compressableProperties = new ConcurrentFactoryDictionary<TypeKey, MemberDetail[]>();
            public static MemberDetail[] GetModelCompressableProperties(Type type, Graph graph)
            {
                var key = new TypeKey(graph?.Signature, type);
                var props = compressableProperties.GetOrAdd(key, (factoryKey) =>
                {
                    var typeDetails = TypeAnalyzer.GetType(type);
                    var propertyDetails = typeDetails.MemberDetails.Where(x => x.Type == typeof(String) || x.Type == typeof(byte[])).ToArray();
                    if (graph != null)
                    {
                        propertyDetails = propertyDetails.Where(x => graph.HasLocalProperty(x.Name)).ToArray();
                    }
                    return propertyDetails;
                });
                return props;
            }
        }
    }
}
