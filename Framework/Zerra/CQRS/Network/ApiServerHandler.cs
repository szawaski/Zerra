// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Network
{
    public static class ApiServerHandler
    {
        public static async Task<ApiResponseData?> HandleRequestAsync(ContentType? contentType, ApiRequestData data)
        {
            if (!String.IsNullOrWhiteSpace(data.ProviderType))
            {
                if (contentType is null)
                    return null;

                var response = await Call(data);

                if (response.Stream is not null)
                {
                    return new ApiResponseData(response.Stream);
                }
                else if (response.Model is not null)
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
                if (data.MessageResult)
                {
                    if (contentType is null)
                        return null;

                    var result = await DispatchWithResult(data);
                    var bytes = ContentTypeSerializer.Serialize(contentType.Value, result);
                    return new ApiResponseData(bytes);
                }
                else
                {
                    await Dispatch(data);
                    return new ApiResponseData();
                }
            }

            return null;
        }

        private static Task<RemoteQueryCallResponse> Call(ApiRequestData data)
        {
            if (String.IsNullOrWhiteSpace(data.ProviderType)) throw new ArgumentNullException(nameof(ApiRequestData.ProviderType));
            if (String.IsNullOrWhiteSpace(data.ProviderMethod)) throw new ArgumentNullException(nameof(ApiRequestData.ProviderMethod));
            if (data.ProviderArguments is null) throw new ArgumentNullException(nameof(ApiRequestData.ProviderArguments));
            if (String.IsNullOrWhiteSpace(data.Source)) throw new ArgumentNullException(nameof(ApiRequestData.Source));

            var providerType = Discovery.GetTypeFromName(data.ProviderType);
            if (!providerType.IsInterface)
                throw new ArgumentException($"Provider {data.ProviderType} is not an interface type");
            var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

            return Bus.RemoteHandleQueryCallAsync(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, true);
        }

        private static Task Dispatch(ApiRequestData data)
        {
            if (String.IsNullOrWhiteSpace(data.MessageType)) throw new ArgumentNullException(nameof(ApiRequestData.MessageType));
            if (data.MessageData is null) throw new ArgumentNullException(nameof(ApiRequestData.MessageData));
            if (String.IsNullOrWhiteSpace(data.Source)) throw new ArgumentNullException(nameof(ApiRequestData.Source));

            var commandType = Discovery.GetTypeFromName(data.MessageType);
            var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);
            if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                throw new Exception($"Type {data.MessageType} is not a command");

            var command = (ICommand?)JsonSerializer.Deserialize(commandType, data.MessageData);
            if (command is null)
                throw new Exception($"Invalid {nameof(data.MessageData)}");

            if (data.MessageAwait)
                return Bus.RemoteHandleCommandDispatchAwaitAsync(command, data.Source, true);
            else
                return Bus.RemoteHandleCommandDispatchAsync(command, data.Source, true);
        }
        private static Task<object?> DispatchWithResult(ApiRequestData data)
        {
            if (String.IsNullOrWhiteSpace(data.MessageType)) throw new ArgumentNullException(nameof(ApiRequestData.MessageType));
            if (data.MessageData is null) throw new ArgumentNullException(nameof(ApiRequestData.MessageData));
            if (String.IsNullOrWhiteSpace(data.Source)) throw new ArgumentNullException(nameof(ApiRequestData.Source));

            var commandType = Discovery.GetTypeFromName(data.MessageType);
            var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);
            if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                throw new Exception($"Type {data.MessageType} is not a command");

            var command = (ICommand?)JsonSerializer.Deserialize(commandType, data.MessageData);
            if (command is null)
                throw new Exception($"Invalid {nameof(data.MessageData)}");

            return Bus.RemoteHandleCommandWithResultDispatchAwaitAsync(command, data.Source, true);
        }
    }
}