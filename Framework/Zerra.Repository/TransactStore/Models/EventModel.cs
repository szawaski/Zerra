﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public sealed class EventModel<TModel> where TModel : class, new()
    {
        public Guid EventID { get; set; }
        public string EventName { get; set; } = null!;

        public DateTime Date { get; set; }
        public ulong Number { get; set; }

        public bool Deleted { get; set; }

        public TModel ModelChange { get; set; } = null!;
        public Graph<TModel>? GraphChange { get; set; }

        public TModel Model { get; set; } = null!;
        public object? Source { get; set; }
        public string? SourceType { get; set; }
    }
}
