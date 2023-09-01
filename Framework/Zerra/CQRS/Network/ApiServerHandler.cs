// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public static class ApiServerHandler
    {
        public static async Task<ApiResponseData> HandleRequestAsync(ContentType? contentType, ApiRequestData data)
        {
            if (!String.IsNullOrWhiteSpace(data.ProviderType))
            {
                if (contentType == null)
                    return null;

                var response = await Call(data);

                if (response.Stream != null)
                {
                    return new ApiResponseData(response.Stream);
                }
                else if (response.Model != null)
                {
                    var bytes = ContentTypeSerializer.Serialize(contentType.Value, response.Model);
                    return new ApiResponseData(bytes);
                }
                else
                {
                    return new ApiResponseData(Array.Empty<byte>());
                }
            }
            else if (!String.IsNullOrWhiteSpace(data.MessageType))
            {
                await Dispatch(data);
                return new ApiResponseData();
            }

            return null;
        }

        private static Task<RemoteQueryCallResponse> Call(ApiRequestData data)
        {
            var providerType = Discovery.GetTypeFromName(data.ProviderType);
            if (!providerType.IsInterface)
                throw new ArgumentException($"Provider {data.ProviderType} is not an interface type");

            var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

            MethodBase method = null;
            foreach (var methodInfo in typeDetail.MethodDetails)
            {
                if (methodInfo.MethodInfo.Name == data.ProviderMethod && methodInfo.ParametersInfo.Count == (data.ProviderArguments != null ? data.ProviderArguments.Length : 0))
                {
                    method = methodInfo.MethodInfo;
                    break;
                }
            }
            if (method == null)
                throw new Exception($"Method {data.ProviderType}.{data.ProviderMethod} does not exsist");

            return Bus.HandleRemoteQueryCallAsync(providerType, data.ProviderMethod, data.ProviderArguments, data.Source);
        }

        private static Task Dispatch(ApiRequestData data)
        {
            var commandType = Discovery.GetTypeFromName(data.MessageType);
            var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);

            if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                throw new Exception($"Type {data.MessageType} is not a command");

            var command = (ICommand)JsonSerializer.Deserialize(data.MessageData, commandType);
            if (data.MessageAwait)
                return Bus.HandleRemoteCommandDispatchAwaitAsync(command, data.Source);
            else
                return Bus.HandleRemoteCommandDispatchAsync(command, data.Source);
        }
    }
}