// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Serialization.Bytes;

namespace Zerra.CQRS.Network
{
    public sealed class Acknowledgement
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Type? ErrorType { get; set; }
        public byte[]? ErrorData { get; set; }

        public Acknowledgement() { }
        public Acknowledgement(Exception? ex)
        {
            ApplyException(this, ex);
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
                ack.ErrorData = null;
            }
            else
            {
                var type = ex.GetType();
                ack.Success = false;
                ack.ErrorMessage = ex.Message;
                ack.ErrorType = type;
                ack.ErrorData = ByteSerializer.Serialize(ex, type, byteSerializerOptions);
            }
        }

        public static void ThrowIfFailed(Acknowledgement? ack)
        {
            if (ack == null)
                throw new RemoteServiceException("Acknowledgement Failed");
            if (ack.Success)
                return;

            Exception? ex = null;
            if (ack.ErrorType != null && ack.ErrorData != null && ack.ErrorData.Length > 0)
            {
                try
                {
                    ex = (Exception?)ByteSerializer.Deserialize(ack.ErrorType, ack.ErrorData, byteSerializerOptions);
                }
                catch { }
            }
            throw new RemoteServiceException(ack.ErrorMessage, ex);
        }
    }
}
