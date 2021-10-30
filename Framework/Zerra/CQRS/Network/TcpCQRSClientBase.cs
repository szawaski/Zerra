// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    public abstract class TcpCQRSClientBase : IQueryClient, ICommandClient
    {
        protected readonly string serviceUrl;
        protected readonly IPEndPoint endpoint;

        string IQueryClient.ConnectionString => serviceUrl;
        string ICommandClient.ConnectionString => serviceUrl;

        public TcpCQRSClientBase(string serviceUrl)
        {
            this.serviceUrl = serviceUrl;
            this.endpoint = IPResolver.GetIPEndPoints(serviceUrl, 80).First();
        }

        private static readonly MethodInfo callRequestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(TcpCQRSClientBase)).MethodDetails.First(x => x.MethodInfo.Name == nameof(TcpCQRSClientBase.CallInternalAsync)).MethodInfo;
        private static readonly Type streamType = typeof(Stream);

        TReturn IQueryClient.Call<TReturn>(Type interfaceType, string methodName, object[] arguments)
        {
            try
            {
                var returnType = typeof(TReturn);

                var returnTypeDetails = TypeAnalyzer.GetTypeDetail(returnType);

                if (returnTypeDetails.IsTask)
                {
                    var isStream = returnTypeDetails.InnerTypeDetails[0].BaseTypes.Contains(streamType);
                    var callRequestMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(callRequestAsyncMethod, returnTypeDetails.InnerTypes.ToArray());
                    return (TReturn)callRequestMethodGeneric.Caller(this, new object[] { isStream, interfaceType, methodName, arguments });
                }
                else
                {
                    var isStream = returnTypeDetails.BaseTypes.Contains(streamType);
                    var model = CallInternal<TReturn>(isStream, interfaceType, methodName, arguments);
                    return model;
                }
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Call Failed", ex);
                throw;
            }
        }

        protected abstract TReturn CallInternal<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments);
        protected abstract Task<TReturn> CallInternalAsync<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments);

        Task ICommandClient.DispatchAsync(ICommand command)
        {
            try
            {
                return DispatchInternal(command, false);
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Dispatch Failed", ex);
                throw;
            }
        }
        Task ICommandClient.DispatchAsyncAwait(ICommand command)
        {
            try
            {
                return DispatchInternal(command, true);
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Dispatch Failed", ex);
                throw;
            }
        }

        protected abstract Task DispatchInternal(ICommand command, bool messageAwait);
    }
}