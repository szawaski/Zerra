// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    public interface IMapLogger
    {
        void LogPropertyChange(string source, string sourceValue, string target, string targetValue);
        void LogPropertyNoChange(string source, string target, string value);
        void LogNewObject(string source, string target, string type);
    }
}
