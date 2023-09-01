// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    public abstract class CQRSClientBase : IQueryClient, ICommandProducer
    {
        protected readonly Uri serviceUrl;

        string IQueryClient.ConnectionString => serviceUrl.OriginalString;
        string ICommandProducer.ConnectionString => serviceUrl.OriginalString;

        public CQRSClientBase(string serviceUrl)
        {
            this.serviceUrl = new Uri(serviceUrl);
        }

        private static readonly MethodInfo callRequestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(CQRSClientBase)).MethodDetails.First(x => x.MethodInfo.Name == nameof(CQRSClientBase.CallInternalAsync)).MethodInfo;
        private static readonly Type streamType = typeof(Stream);

        TReturn IQueryClient.Call<TReturn>(Type interfaceType, string methodName, object[] arguments, string source)
        {
            try
            {
                var returnType = typeof(TReturn);

                var returnTypeDetails = TypeAnalyzer.GetTypeDetail(returnType);

                if (returnTypeDetails.IsTask)
                {
                    var isStream = returnTypeDetails.InnerTypeDetails[0].BaseTypes.Contains(streamType);
                    var callRequestMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(callRequestAsyncMethod, returnTypeDetails.InnerTypes.ToArray());
                    return (TReturn)callRequestMethodGeneric.Caller(this, new object[] { isStream, interfaceType, methodName, arguments, source });
                }
                else
                {
                    var isStream = returnTypeDetails.BaseTypes.Contains(streamType);
                    var model = CallInternal<TReturn>(isStream, interfaceType, methodName, arguments, source);
                    return model;
                }
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Call Failed", ex);
                throw;
            }
        }

        protected abstract TReturn CallInternal<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments, string source);
        protected abstract Task<TReturn> CallInternalAsync<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments, string source);

        Task ICommandProducer.DispatchAsync(ICommand command, string source)
        {
            try
            {
                return DispatchInternal(command, false, source);
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Dispatch Failed", ex);
                throw;
            }
        }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command, string source)
        {
            try
            {
                return DispatchInternal(command, true, source);
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Dispatch Failed", ex);
                throw;
            }
        }

        protected abstract Task DispatchInternal(ICommand command, bool messageAwait, string source);
    }
}