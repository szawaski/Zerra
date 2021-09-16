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
    public class FinalBlockStream : StreamWrapper
    {
        private readonly CryptoStream cryptoStream;
        private readonly CryptoShiftStream cryptoShiftStream;
        public FinalBlockStream(CryptoStream stream, bool leaveOpen) : base(stream, leaveOpen)
        {
            this.cryptoStream = stream;
            this.cryptoShiftStream = null;
        }
        public FinalBlockStream(CryptoShiftStream stream, bool leaveOpen) : base(stream, leaveOpen)
        {
            this.cryptoStream = null;
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
    }
}