// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO.Compression;
using System.Text;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseTransactStoreCompressionProvider<TNextProviderInterface, TModel>
    {
        private static class CompressionCommon
        {
            private const string compressionPrefix = "<c>";

            /// <summary>Compresses a plain text string using GZip and returns a Base64-encoded compressed string with a compression prefix.</summary>
            /// <param name="plain">The plain text string to compress.</param>
            /// <returns>The compressed string, or the original value if it is already compressed.</returns>
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
            /// <summary>Compresses a byte array using GZip.</summary>
            /// <param name="plain">The byte array to compress.</param>
            /// <returns>The GZip-compressed byte array.</returns>
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
            /// <summary>Creates a <see cref="GZipStream"/> for compressing data written to the given stream.</summary>
            /// <param name="stream">The destination stream to write compressed data to.</param>
            /// <returns>A <see cref="GZipStream"/> wrapping the destination stream.</returns>
            public static GZipStream CompressGZip(Stream stream)
            {
                var gzipStream = new GZipStream(stream, CompressionLevel.Fastest);
                return gzipStream;
            }

            /// <summary>Decompresses a GZip-compressed, Base64-encoded string back to plain text.</summary>
            /// <param name="compressed">The compressed string to decompress.</param>
            /// <returns>The decompressed plain text string, or the original value if it is not compressed.</returns>
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
            /// <summary>Decompresses a GZip-compressed byte array.</summary>
            /// <param name="compressed">The compressed byte array to decompress.</param>
            /// <returns>The decompressed byte array.</returns>
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
            /// <summary>Creates a <see cref="GZipStream"/> for decompressing data read from the given stream.</summary>
            /// <param name="stream">The source stream containing compressed data.</param>
            /// <returns>A <see cref="GZipStream"/> wrapping the source stream.</returns>
            public static GZipStream DecompressGZip(Stream stream)
            {
                var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                return gzipStream;
            }

            private static readonly ConcurrentFactoryDictionary<TypeKey, MemberDetail[]> compressableProperties = new();
            /// <summary>Returns the model members eligible for compression (string and byte[] properties), optionally filtered by a graph.</summary>
            /// <param name="type">The model type to inspect.</param>
            /// <param name="graph">An optional graph to restrict which members are returned.</param>
            /// <returns>An array of <see cref="MemberDetail"/> representing the compressable properties.</returns>
            public static MemberDetail[] GetModelCompressableProperties(Type type, Graph? graph)
            {
                var key = new TypeKey(graph?.Signature, type);
                var props = compressableProperties.GetOrAdd(key, type, graph, static (type, graph) =>
                {
                    var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                    var propertyDetails = typeDetails.Members.Where(x => x.Type == typeof(string) || x.Type == typeof(byte[])).ToArray();
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
