﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class TypeModel
    {
        public Type Type1 { get; set; }
        public Type Type2 { get; set; }
        public Type Type3 { get; set; }

        public static TypeModel Create()
        {
            var model = new TypeModel()
            {
                Type1 = typeof(bool),
                Type2 = typeof(byte[]),
                Type3 = typeof(List<string>),
            };
            return model;
        }
    }
}
