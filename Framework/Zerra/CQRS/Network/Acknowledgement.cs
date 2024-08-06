// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes;

namespace Zerra.CQRS.Network
{
    public sealed class Acknowledgement
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Type? ErrorType { get; set; }
        public byte[]? Data { get; set; }

        public Acknowledgement(string errorMessage)
        {
            this.Success = false;
            this.ErrorMessage = errorMessage;
        }
        public Acknowledgement(object? result, Exception? ex)
        {
            this.Success = ex is null;
            if (ex is not null)
            {
                this.Success = false;
                this.ErrorMessage = ex.Message;
                this.ErrorType = ex.GetType();
                this.Data = ByteSerializer.Serialize(ex, this.ErrorType, byteSerializerOptions);
            }
            else
            {
                this.Success = true;
                this.Data = ByteSerializer.Serialize(result, byteSerializerOptions);
            }
        }

        private static readonly ByteSerializerOptions byteSerializerOptions = new()
        {
            UsePropertyNames = true,
            IgnoreIndexAttribute = true
        };

        public static void ApplyException(Acknowledgement? ack, Exception? ex)
        {
            if (ack == null)
                return;

            if (ex == null)
            {
                ack.Success = true;
                ack.ErrorMessage = null;
                ack.ErrorType = null;
                ack.Data = null;
            }
            else
            {
                var type = ex.GetType();
                ack.Success = false;
                ack.ErrorMessage = ex.Message;
                ack.ErrorType = type;
                ack.Data = ByteSerializer.Serialize(ex, type, byteSerializerOptions);
            }
        }

        public static void ThrowIfFailed(Acknowledgement? ack)
        {
            if (ack == null)
                throw new RemoteServiceException("Acknowledgement Failed");
            if (ack.Success)
                return;

            Exception? ex = null;
            if (ack.ErrorType != null && ack.Data != null && ack.Data.Length > 0)
            {
                try
                {
                    ex = (Exception?)ByteSerializer.Deserialize(ack.ErrorType, ack.Data, byteSerializerOptions);
                }
                catch { }
            }
            throw new RemoteServiceException(ack.ErrorMessage, ex);
        }
    }
}
