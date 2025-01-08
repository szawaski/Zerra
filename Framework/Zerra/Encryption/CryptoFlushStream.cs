// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
#if NET5_0_OR_GREATER
using System.Threading.Tasks;
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
        private readonly CryptoShiftStream? cryptoShiftStream;
        internal CryptoFlushStream(CryptoStream stream, ICryptoTransform transform, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.cryptoStream = stream;
            this.transform = transform;
            this.cryptoShiftStream = null;
        }
        internal CryptoFlushStream(CryptoShiftStream stream, ICryptoTransform transform, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.cryptoStream = null;
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
            else if (cryptoShiftStream is not null)
                cryptoShiftStream.FlushFinalBlock();
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Calls the underlying FlushFinalBlockAsync
        /// </summary>
        public ValueTask FlushFinalBlockAsync()
        {
            if (cryptoStream is not null)
                return cryptoStream.FlushFinalBlockAsync();
            else if (cryptoShiftStream is not null)
                return cryptoShiftStream.FlushFinalBlockAsync();
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