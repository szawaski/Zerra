// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;

#if !NETSTANDARD2_0
#endif
using Zerra.IO;

namespace Zerra.Encryption
{
    /// <summary>
    /// Abstracts different streams that require calling FlushFinalBlockAsync.
    /// </summary>
    public sealed class CryptoFlushStream : StreamWrapper
    {
        private readonly CryptoStream? cryptoStream;
        private readonly ICryptoTransform? transform;
        private readonly CryptoChunkStream? cryptoChunkStream;
        private readonly CryptoShiftStream? cryptoShiftStream;
        internal CryptoFlushStream(CryptoStream stream, ICryptoTransform transform, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.cryptoStream = stream;
            this.transform = transform;
        }
        internal CryptoFlushStream(CryptoChunkStream stream)
            : base(stream, false)
        {
            this.cryptoChunkStream = stream;
        }
        internal CryptoFlushStream(CryptoShiftStream stream, ICryptoTransform transform, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.transform = transform;
            this.cryptoShiftStream = stream;
        }

        /// <summary>
        /// Calls the underlying FlushFinalBlockAsync
        /// </summary>
        public void FlushFinalBlock()
        {
            if (cryptoStream is not null)
                cryptoStream.FlushFinalBlock();
            else if (cryptoChunkStream is not null)
                cryptoChunkStream.FlushFinalBlock();
            else if (cryptoShiftStream is not null)
                cryptoShiftStream.FlushFinalBlock();
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Calls the underlying FlushFinalBlockAsync
        /// </summary>
        public ValueTask FlushFinalBlockAsync(CancellationToken cancellationToken = default)
        {
            if (cryptoStream is not null)
                return cryptoStream.FlushFinalBlockAsync(cancellationToken);
            else if (cryptoChunkStream is not null)
                return cryptoChunkStream.FlushFinalBlockAsync(cancellationToken);
            else if (cryptoShiftStream is not null)
                return cryptoShiftStream.FlushFinalBlockAsync(cancellationToken);
            return ValueTask.CompletedTask;
        }
#endif

        ///<inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            transform?.Dispose();
        }
    }
}