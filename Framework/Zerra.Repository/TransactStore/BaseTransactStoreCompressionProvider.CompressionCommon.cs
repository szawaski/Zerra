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
                using (var msIn = new MemoryStream(plain))
                using (var msOut = new MemoryStream())
                {
                    using (var gzipStream = CompressGZip(msOut))
                    {
                        msIn.CopyTo(gzipStream);
                    }
                    var compressed = msOut.ToArray();
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
                string compressedWithoutPrefix;
                if (compressed.Length > compressionPrefix.Length && compressed.Substring(0, compressionPrefix.Length) == compressionPrefix)
                    compressedWithoutPrefix = compressed.Substring(compressionPrefix.Length);
                else
                    return compressed;

                var compressedBytes = Convert.FromBase64String(compressedWithoutPrefix);
                var plainBytes = DecompressGZip(compressedBytes);
                var plain = Encoding.Unicode.GetString(plainBytes);
                return plain;
            }
            public static byte[] DecompressGZip(byte[] compressed)
            {
                using (var msIn = new MemoryStream(compressed))
                using (var msOut = new MemoryStream())
                {
                    using (var gzipStream = DecompressGZip(msIn))
                    {
                        gzipStream.CopyTo(msOut);
                    }
                    var plain = msOut.ToArray();
                    return plain;
                }
            }
            public static GZipStream DecompressGZip(Stream stream)
            {
                var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                return gzipStream;
            }

            private static readonly ConcurrentFactoryDictionary<TypeKey, MemberDetail[]> compressableProperties = new();
            public static MemberDetail[] GetModelCompressableProperties(Type type, Graph? graph)
            {
                var key = new TypeKey(graph?.Signature, type);
                var props = compressableProperties.GetOrAdd(key, (_) =>
                {
                    var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                    var propertyDetails = typeDetails.MemberDetails.Where(x => x.Type == typeof(string) || x.Type == typeof(byte[])).ToArray();
                    if (graph is not null)
                    {
                        propertyDetails = propertyDetails.Where(x => graph.HasMember(x.Name)).ToArray();
                    }
                    return propertyDetails;
                });
                return props;
            }
        }
    }
}
