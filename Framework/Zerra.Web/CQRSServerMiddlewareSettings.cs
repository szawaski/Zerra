// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.CQRS;
using Zerra.CQRS.Network;

namespace Zerra.Web
{
    public class CQRSServerMiddlewareSettings
    {
        public List<Type> InterfaceTypes { get; private set; }
        public List<Type> CommandTypes { get; private set; }

        public Func<Type, string, string[], Task<RemoteQueryCallResponse>> ProviderHandlerAsync { get; set; }
        public Func<ICommand, Task> HandlerAsync { get; set; }
        public Func<ICommand, Task> HandlerAwaitAsync { get; set; }

        public string Route { get; set; }
        public ContentType ContentType { get; set; }
        public IHttpAuthorizer HttpAuthorizer { get; set; }

        private string[] allowOrigins;
        public string[] AllowOrigins
        {
            get
            {
                return allowOrigins;
            }
            set
            {
                if (value != null && !value.Contains("*"))
                {
                    allowOrigins = value;
                    allowOriginsString = value != null ? String.Join(", ", value) : "*";
                }
                else
                {
                    allowOrigins = null;
                    allowOriginsString = null;
                }
            }
        }

        private string allowOriginsString;
        public string AllowOriginsString
        {
            get
            {
                return allowOriginsString;
            }
        }

        public CQRSServerMiddlewareSettings()
        {
            InterfaceTypes = new List<Type>();
            CommandTypes = new List<Type>();
        }
    }
}