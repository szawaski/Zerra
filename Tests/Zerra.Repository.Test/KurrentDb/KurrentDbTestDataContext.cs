// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.KurrentDB;

namespace Zerra.Repository.Test.KurrentDb
{
    public class KurrentDbTestDataContext : KurrentDbDataContext
    {
        public override string ConnectionString => "http://localhost:2113";
        public override bool Insecure => true;
    }
}
