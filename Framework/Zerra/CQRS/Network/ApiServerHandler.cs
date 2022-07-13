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
        public static async Task<ApiResponseData> HandleRequestAsync(ContentType? contentType, CQRSRequestData data)
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

        private static Task<RemoteQueryCallResponse> Call(CQRSRequestData data)
        {
            var providerType = Discovery.GetTypeFromName(data.ProviderType);
            if (!providerType.IsInterface)
                throw new ArgumentException($"Provider {data.ProviderType} is not an interface type");

            var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);
            var exposed = typeDetail.Attributes.Any(x => x is ServiceExposedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == NetworkType.Api))
                && !typeDetail.Attributes.Any(x => x is ServiceBlockedAttribute attribute && (attribute.NetworkType == NetworkType.Api || !attribute.NetworkType.HasValue));

            MethodBase method = null;
            foreach (var methodInfo in typeDetail.MethodDetails)
            {
                if (methodInfo.MethodInfo.Name == data.ProviderMethod && methodInfo.ParametersInfo.Count == (data.ProviderArguments != null ? data.ProviderArguments.Length : 0))
                {
                    if (!exposed && (!methodInfo.Attributes.Any(x => x is ServiceExposedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == NetworkType.Api)) || methodInfo.Attributes.Any(x => x is ServiceBlockedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == NetworkType.Api))))
                        throw new Exception($"Method {data.ProviderType}.{data.ProviderMethod} is not exposed to {NetworkType.Api}");
                    method = methodInfo.MethodInfo;
                    break;
                }
            }
            if (method == null)
                throw new Exception($"Method {data.ProviderType}.{data.ProviderMethod} does not exsist");

            return Bus.HandleRemoteQueryCallAsync(providerType, data.ProviderMethod, data.ProviderArguments);
        }

        private static Task Dispatch(CQRSRequestData data)
        {
            var commandType = Discovery.GetTypeFromName(data.MessageType);
            var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);

            if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                throw new Exception($"Type {data.MessageType} is not a command");

            var exposed = typeDetail.Attributes.Any(x => x is ServiceExposedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == NetworkType.Api))
                && !typeDetail.Attributes.Any(x => x is ServiceBlockedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == NetworkType.Api));
            if (!exposed)
                throw new Exception($"Command {data.MessageType} is not exposed to {NetworkType.Api}");

            var command = (ICommand)JsonSerializer.Deserialize(data.MessageData, commandType);
            if (data.MessageAwait)
                return Bus.HandleRemoteCommandDispatchAwaitAsync(command);
            else
                return Bus.HandleRemoteCommandDispatchAsync(command);
        }
    }
}