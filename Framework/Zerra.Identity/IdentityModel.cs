// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Identity
{
    public class IdentityModel
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string[] Roles { get; set; }
        public string ServiceProvider { get; set; }
        public Dictionary<string, string> OtherClaims { get; set; }
        public string State { get; set; }
        public string AccessToken { get; set; }
    }
}
