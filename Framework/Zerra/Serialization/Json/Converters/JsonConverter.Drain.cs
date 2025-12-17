// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters
{
    public abstract partial class JsonConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainFromParent(ref JsonReader reader, ref ReadState state)
        {
            if (state.Current.ChildJsonToken == JsonToken.NotDetermined)
            {
                state.Current.ChildJsonToken = reader.Token;
            }

            switch (state.Current.ChildJsonToken)
            {
                case JsonToken.ObjectStart:
                    state.PushFrame(null);
                    if (!DrainObject(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.ArrayStart:
                    state.PushFrame(null);
                    if (!DrainArray(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.String:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.Null:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.False:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.True:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.Number:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                default:
                    throw reader.CreateException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainFromParentMember(ref JsonReader reader, ref ReadState state)
        {
            if (state.Current.ChildJsonToken == JsonToken.NotDetermined)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                    return false;
                state.Current.ChildJsonToken = reader.Token;
            }

            switch (state.Current.ChildJsonToken)
            {
                case JsonToken.ObjectStart:
                    state.PushFrame(null);
                    if (!DrainObject(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.ArrayStart:
                    state.PushFrame(null);
                    if (!DrainArray(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.String:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.Null:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.False:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.True:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.Number:
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                default:
                    throw reader.CreateException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool Drain(ref JsonReader reader, ref ReadState state, JsonToken token)
        {
            switch (token)
            {
                case JsonToken.ObjectStart:
                    return DrainObject(ref reader, ref state);
                case JsonToken.ArrayStart:
                    return DrainArray(ref reader, ref state);
                case JsonToken.String:
                    return true;
                case JsonToken.Null:
                    return true;
                case JsonToken.False:
                    return true;
                case JsonToken.True:
                    return true;
                case JsonToken.Number:
                    return true;
                default:
                    throw reader.CreateException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainObject(ref JsonReader reader, ref ReadState state)
        {
            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.SizeNeeded = 1;
                    return false;
                }
                state.Current.HasReadFirstToken = true;

                if (reader.Token == JsonToken.ObjectEnd)
                    return true;

                state.Current.HasCreated = true;
            }

            for (; ; )
            {
                if (!state.Current.HasReadFirstToken)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        return false;
                    }
                }

                if (!state.Current.HasReadSeperator)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.HasReadProperty = true;
                        return false;
                    }
                    if (reader.Token != JsonToken.PropertySeperator)
                        throw reader.CreateException();
                }

                if (!state.Current.HasReadValue)
                {
                    if (!DrainFromParentMember(ref reader, ref state))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        return false;
                    }
                }

                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.Current.HasReadFirstToken = true;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadSeperator = true;
                    state.Current.HasReadValue = true;
                    return false;
                }

                if (reader.Token == JsonToken.ObjectEnd)
                    break;

                if (reader.Token != JsonToken.NextItem)
                    throw reader.CreateException();

                state.Current.HasReadFirstToken = false;
                state.Current.HasReadProperty = false;
                state.Current.HasReadSeperator = false;
                state.Current.HasReadValue = false;
            }

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainArray(ref JsonReader reader, ref ReadState state)
        {
            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.SizeNeeded = 1;
                    return false;
                }
                state.Current.HasReadFirstToken = true;

                if (reader.Token == JsonToken.ArrayEnd)
                {
                    return true;
                }
            }

            for (; ; )
            {
                if (!state.Current.HasReadFirstToken)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasCreated = true;
                        return false;
                    }
                }

                if (!state.Current.HasReadValue)
                {
                    if (!DrainFromParent(ref reader, ref state))
                    {
                        state.Current.HasCreated = true;
                        state.Current.HasReadFirstToken = true;
                        return false;
                    }
                }

                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.Current.HasCreated = true;
                    state.Current.HasReadFirstToken = true;
                    state.Current.HasReadValue = true;
                    return false;
                }

                if (reader.Token == JsonToken.ArrayEnd)
                    return true;

                if (reader.Token != JsonToken.NextItem)
                    throw reader.CreateException();

                state.Current.HasReadFirstToken = false;
                state.Current.HasReadValue = false;
            }
        }
    }
}
