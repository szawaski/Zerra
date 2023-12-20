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
    public sealed class CryptoFlushStream : StreamWrapper
    {
        private readonly CryptoStream? cryptoStream;
        private readonly ICryptoTransform transform;
        private readonly CryptoShiftStream? cryptoShiftStream;
        public CryptoFlushStream(CryptoStream stream, ICryptoTransform transform, bool leaveOpen) : base(stream, leaveOpen)
        {
            this.cryptoStream = stream;
            this.transform = transform;
            this.cryptoShiftStream = null;
        }
        public CryptoFlushStream(CryptoShiftStream stream, ICryptoTransform transform, bool leaveOpen) : base(stream, leaveOpen)
        {
            this.cryptoStream = null;
            this.transform = transform;
            this.cryptoShiftStream = stream;
        }

        public void FlushFinalBlock()
        {
            if (cryptoStream != null)
                cryptoStream.FlushFinalBlock();
            else if (cryptoShiftStream != null)
                cryptoShiftStream.FlushFinalBlock();
        }

#if NET5_0_OR_GREATER
        public ValueTask FlushFinalBlockAsync()
        {
            if (cryptoStream != null)
                return cryptoStream.FlushFinalBlockAsync();
            else if (cryptoShiftStream != null)
                return cryptoShiftStream.FlushFinalBlockAsync();
            return ValueTask.CompletedTask;
        }
#endif

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            transform.Dispose();
        }
    }
}