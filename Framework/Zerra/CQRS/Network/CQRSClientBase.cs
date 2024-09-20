// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    public abstract class CqrsClientBase : IQueryClient, ICommandProducer, IDisposable
    {
        protected readonly Uri serviceUri;
        protected readonly string host;
        protected readonly int port;
        private readonly ConcurrentDictionary<Type, SemaphoreSlim> throttleByInterfaceType;
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;

        public CqrsClientBase(string serviceUrl)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentNullException(nameof(serviceUrl));

            if (!serviceUrl.Contains("://"))
                this.serviceUri = new Uri("any://" + serviceUrl); //hacky way to make it parse without scheme.
            else
                this.serviceUri = new Uri(serviceUrl, UriKind.RelativeOrAbsolute);
            host = this.serviceUri.Host;
            port = this.serviceUri.Port >= 0 ? this.serviceUri.Port : (String.Equals(this.serviceUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ? 443 : 80);

            this.throttleByInterfaceType = new();
            this.topicsByCommandType = new();
            this.throttleByTopic = new();
        }

        private static readonly MethodInfo callRequestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(CqrsClientBase)).MethodDetailsBoxed.First(x => x.MethodInfo.Name == nameof(CqrsClientBase.CallInternalAsync)).MethodInfo;
        private static readonly Type streamType = typeof(Stream);

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByCommandType.ContainsKey(type))
                return;
            topicsByCommandType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }
        IEnumerable<Type> ICommandProducer.GetCommandTypes()
        {
            return topicsByCommandType.Keys;
        }

        void IQueryClient.RegisterInterfaceType(int maxConcurrent, Type type)
        {
            if (throttleByInterfaceType.ContainsKey(type))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByInterfaceType.TryAdd(type, throttle))
                throttle.Dispose();
        }

        TReturn? IQueryClient.Call<TReturn>(Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            if (!throttleByInterfaceType.TryGetValue(interfaceType, out var throttle))
                throw new Exception($"{interfaceType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");

            try
            {
                var returnTypeDetails = TypeAnalyzer<TReturn>.GetTypeDetail();

                if (returnTypeDetails.IsTask)
                {
                    var isStream = returnTypeDetails.InnerTypeDetail.BaseTypes.Contains(streamType);
                    var callRequestMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(callRequestAsyncMethod, returnTypeDetails.InnerTypes.ToArray());
                    return (TReturn?)callRequestMethodGeneric.Caller(this, [throttle, isStream, interfaceType, methodName, arguments, source])!;
                }
                else
                {
                    var isStream = returnTypeDetails.BaseTypes.Contains(streamType);
                    var model = CallInternal<TReturn>(throttle, isStream, interfaceType, methodName, arguments, source);
                    return model;
                }
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Call Failed", ex);
                throw;
            }
        }

        protected abstract TReturn? CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source);
        protected abstract Task<TReturn?> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source);

        Task ICommandProducer.DispatchAsync(ICommand command, string source)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");

            try
            {
                return DispatchInternal(throttle, commandType, command, false, source);
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Dispatch Failed", ex);
                throw;
            }
        }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command, string source)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");

            try
            {
                return DispatchInternal(throttle, commandType, command, true, source);
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Dispatch Failed", ex);
                throw;
            }
        }
        Task<TResult?> ICommandProducer.DispatchAsyncAwait<TResult>(ICommand<TResult> command, string source) where TResult : default
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");

            var resultTypeDetails = TypeAnalyzer<TResult>.GetTypeDetail();
            var isStream = resultTypeDetails.BaseTypes.Contains(streamType);

            try
            {
                return DispatchInternal<TResult>(throttle, isStream, commandType, command, source);
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Dispatch Failed", ex);
                throw;
            }
        }

        protected abstract Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source);
        protected abstract Task<TResult?> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source);

        public void Dispose()
        {
            foreach (var throttle in throttleByInterfaceType.Values)
                throttle.Dispose();
            foreach (var throttle in throttleByTopic.Values)
                throttle.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}