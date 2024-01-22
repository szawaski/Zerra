// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Test.Map
{
    public class MapperLog : IMapLogger
    {
        private readonly List<string> log = new();

        public void LogPropertyChange(string source, string sourceValue, string target, string targetValue)
        {
            log.Add($"Changed {targetValue} To {sourceValue} - {target} to {source}");
        }

        public void LogPropertyNoChange(string source, string target, string targetValue)
        {
            log.Add($"No Change {targetValue} - {target} to {source}");
        }

        public void LogNewObject(string source, string target, string type)
        {
            log.Add($"New {type} On {target}");
        }
    }
}
