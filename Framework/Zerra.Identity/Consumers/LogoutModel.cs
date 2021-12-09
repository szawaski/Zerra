// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Identity.Consumers
{
    public class LogoutModel
    {
        public string ServiceProvider { get; set; }
        public string State { get; set; }
        public Dictionary<string, string> OtherClaims { get; set; }
    }
}
