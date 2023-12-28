// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public static class ApiServerHandler
    {
        public static async Task<ApiResponseData?> HandleRequestAsync(ContentType? contentType, ApiRequestData data)
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
            if (String.IsNullOrWhiteSpace(data.ProviderType)) throw new ArgumentNullException(nameof(ApiRequestData.ProviderType));
            if (String.IsNullOrWhiteSpace(data.ProviderMethod)) throw new ArgumentNullException(nameof(ApiRequestData.ProviderMethod));
            if (data.ProviderArguments == null) throw new ArgumentNullException(nameof(ApiRequestData.ProviderArguments));
            if (String.IsNullOrWhiteSpace(data.Source)) throw new ArgumentNullException(nameof(ApiRequestData.Source));

            var providerType = Discovery.GetTypeFromName(data.ProviderType);
            if (!providerType.IsInterface)
                throw new ArgumentException($"Provider {data.ProviderType} is not an interface type");
            var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

            var exposed = false;
            foreach (var attribute in typeDetail.Attributes)
            {
                if (attribute is ServiceExposedAttribute item && item.NetworkType >= NetworkType.Api)
                    exposed = true;
            }
            if (!exposed)
                throw new Exception($"Interface {typeDetail.Type.GetNiceName()} is not exposed");

            MethodDetail? methodDetail = null;
            foreach (var method in typeDetail.MethodDetails)
            {
                if (method.MethodInfo.Name == data.ProviderMethod && method.ParametersInfo.Count == data.ProviderArguments.Length)
                {
                    methodDetail = method;
                    break;
                }
            }
            if (methodDetail == null)
                throw new Exception($"Method {data.ProviderType}.{data.ProviderMethod} does not exsist");

            var blockedMethod = false;
            foreach (var attribute in methodDetail.Attributes)
            {
                if (attribute is ServiceBlockedAttribute item && item.NetworkType >= NetworkType.Api)
                    blockedMethod = true;
            }
            if (blockedMethod)
                throw new Exception($"Method {typeDetail.Type.GetNiceName()}.{methodDetail.Name} is blocked for {nameof(NetworkType)}.{nameof(NetworkType.Api)}");

            return Bus.HandleRemoteQueryCallAsync(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, true);
        }

        private static Task Dispatch(ApiRequestData data)
        {
            if (String.IsNullOrWhiteSpace(data.MessageType)) throw new ArgumentNullException(nameof(ApiRequestData.MessageType));
            if (data.MessageData == null) throw new ArgumentNullException(nameof(ApiRequestData.MessageData));
            if (String.IsNullOrWhiteSpace(data.Source)) throw new ArgumentNullException(nameof(ApiRequestData.Source));

            var commandType = Discovery.GetTypeFromName(data.MessageType);
            var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);
            if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                throw new Exception($"Type {data.MessageType} is not a command");

            var exposed = false;
            foreach (var attribute in typeDetail.Attributes)
            {
                if (attribute is ServiceExposedAttribute item && item.NetworkType >= NetworkType.Api)
                    exposed = true;
            }
            if (!exposed)
                throw new Exception($"{typeDetail.Type.GetNiceName()} is not exposed");

            var command = (ICommand?)JsonSerializer.Deserialize(commandType, data.MessageData);
            if (command == null)
                throw new Exception($"Invalid {nameof(data.MessageData)}");

            if (data.MessageAwait == true)
                return Bus.HandleRemoteCommandDispatchAwaitAsync(command, data.Source, true);
            else
                return Bus.HandleRemoteCommandDispatchAsync(command, data.Source, true);
        }
    }
}