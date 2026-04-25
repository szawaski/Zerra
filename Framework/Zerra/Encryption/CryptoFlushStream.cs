// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;

#if NET5_0_OR_GREATER
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
        private readonly ICryptoTransform transform;
        private readonly CryptoPrefixStream? cryptoPrefixStream;
#pragma warning disable CS0612 // Type or member is obsolete
        private readonly CryptoShiftStream? cryptoShiftStream;
#pragma warning restore CS0612 // Type or member is obsolete
        internal CryptoFlushStream(CryptoStream stream, ICryptoTransform transform, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.cryptoStream = stream;
            this.transform = transform;
        }
        internal CryptoFlushStream(CryptoPrefixStream stream, ICryptoTransform transform, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.transform = transform;
            this.cryptoPrefixStream = stream;
        }
#pragma warning disable CS0612 // Type or member is obsolete
        internal CryptoFlushStream(CryptoShiftStream stream, ICryptoTransform transform, bool leaveOpen)
#pragma warning restore CS0612 // Type or member is obsolete
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
            else if (cryptoPrefixStream is not null)
                cryptoPrefixStream.FlushFinalBlock();
            else if (cryptoShiftStream is not null)
                cryptoShiftStream.FlushFinalBlock();
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Calls the underlying FlushFinalBlockAsync
        /// </summary>
        public ValueTask FlushFinalBlockAsync(CancellationToken cancellationToken = default)
        {
            if (cryptoStream is not null)
                return cryptoStream.FlushFinalBlockAsync(cancellationToken);
            else if (cryptoPrefixStream is not null)
                _ = cryptoPrefixStream.FlushFinalBlockAsync(cancellationToken);
            else if (cryptoShiftStream is not null)
                return cryptoShiftStream.FlushFinalBlockAsync(cancellationToken);
            return ValueTask.CompletedTask;
        }
#endif

        ///<inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            transform.Dispose();
        }
    }
}