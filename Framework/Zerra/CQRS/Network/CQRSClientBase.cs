// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    public abstract class CqrsClientBase : IQueryClient, ICommandProducer, IEventProducer, IDisposable
    {
        private readonly string serviceUrl;
        protected readonly Uri serviceUri;
        protected readonly string host;
        protected readonly int port;
        private readonly ConcurrentDictionary<Type, SemaphoreSlim> throttleByInterfaceType;
        private readonly ConcurrentDictionary<Type, string> topicsByMessageType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;

        public CqrsClientBase(string serviceUrl)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentNullException(nameof(serviceUrl));

            this.serviceUrl = serviceUrl;
            if (!serviceUrl.Contains("://"))
                this.serviceUri = new Uri($"tcp://{serviceUrl}"); //hacky way to make it parse without scheme.
            else
                this.serviceUri = new Uri(serviceUrl, UriKind.RelativeOrAbsolute);
            host = this.serviceUri.Host;
            port = this.serviceUri.Port >= 0 ? this.serviceUri.Port : (String.Equals(this.serviceUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ? 443 : 80);

            this.throttleByInterfaceType = new();
            this.topicsByMessageType = new();
            this.throttleByTopic = new();
        }

        private static readonly MethodInfo callRequestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(CqrsClientBase)).MethodDetailsBoxed.First(x => x.MethodInfo.Name == nameof(CqrsClientBase.CallInternalAsync)).MethodInfo;
        private static readonly Type streamType = typeof(Stream);

        string IQueryClient.ServiceUrl => serviceUrl;
        string ICommandProducer.MessageHost => serviceUrl;
        string IEventProducer.MessageHost => serviceUrl;

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByMessageType.ContainsKey(type))
                return;
            topicsByMessageType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByMessageType.ContainsKey(type))
                return;
            topicsByMessageType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
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
                    var isStream = returnTypeDetails.InnerType == streamType || returnTypeDetails.InnerTypeDetail.BaseTypes.Contains(streamType);
                    var callRequestMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(callRequestAsyncMethod, returnTypeDetails.InnerTypes.ToArray());
                    return (TReturn?)callRequestMethodGeneric.CallerBoxed(this, [throttle, isStream, interfaceType, methodName, arguments, source])!;
                }
                else
                {
                    var isStream = returnTypeDetails.InnerType == streamType || returnTypeDetails.InnerTypeDetail.BaseTypes.Contains(streamType);
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
            if (!topicsByMessageType.TryGetValue(commandType, out var topic))
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
        Task ICommandProducer.DispatchAwaitAsync(ICommand command, string source)
        {
            var commandType = command.GetType();
            if (!topicsByMessageType.TryGetValue(commandType, out var topic))
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
        Task<TResult?> ICommandProducer.DispatchAwaitAsync<TResult>(ICommand<TResult> command, string source) where TResult : default
        {
            var commandType = command.GetType();
            if (!topicsByMessageType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");

            var resultTypeDetails = TypeAnalyzer<TResult>.GetTypeDetail();
            var isStream = resultTypeDetails.Type == streamType || resultTypeDetails.BaseTypes.Contains(streamType);

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

        Task IEventProducer.DispatchAsync(IEvent @event, string source)
        {
            var commandType = @event.GetType();
            if (!topicsByMessageType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");

            try
            {
                return DispatchInternal(throttle, commandType, @event, source);
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Dispatch Failed", ex);
                throw;
            }
        }

        protected abstract Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source);
        protected abstract Task<TResult?> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source);

        protected abstract Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source);

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