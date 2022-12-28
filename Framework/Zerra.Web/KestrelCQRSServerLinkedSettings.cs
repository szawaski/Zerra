﻿// Copyright © KaKush LLC
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
    public class KestrelCQRSServerLinkedSettings
    {
        public List<Type> InterfaceTypes { get; private set; }
        public List<Type> CommandTypes { get; private set; }

        public Func<Type, string, string[], Task<RemoteQueryCallResponse>> ProviderHandlerAsync { get; set; }
        public Func<ICommand, Task> HandlerAsync { get; set; }
        public Func<ICommand, Task> HandlerAwaitAsync { get; set; }

        public string Route { get; set; }
        public ContentType ContentType { get; set; }
        public ICQRSAuthorizer Authorizer { get; set; }

        private string[] allowOrigins;
        public string[] AllowOrigins
        {
            get
            {
                return allowOrigins;
            }
            set
            {
                allowOrigins = value == null || value.Length == 0 ? null : value;
                allowOriginsString = value == null || value.Length == 0 ? "*" : String.Join(", ", value);
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

        public KestrelCQRSServerLinkedSettings()
        {
            InterfaceTypes = new();
            CommandTypes = new();
            this.allowOriginsString = "*";
        }
    }
}