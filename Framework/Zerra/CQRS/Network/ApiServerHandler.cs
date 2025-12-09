// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// Helper class to handle external API requests.
    /// Used by Zerra.Web.CqrsApiGatewayMiddleware
    /// </summary>
    public static class ApiServerHandler
    {
        /// <summary>
        /// Handles an external API request.
        /// </summary>
        /// <param name="contentType">The serialized content type of the request.</param>
        /// <param name="data">The data received for the API request.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The response from the API request.</returns>
        public static async Task<ApiResponseData?> HandleRequestAsync(IBus bus, ISerializer serializer, ApiRequestData data, CancellationToken cancellationToken)
        {
            if (!String.IsNullOrWhiteSpace(data.ProviderType))
            {
                var response = await Call(bus, serializer, data, cancellationToken);

                if (response.Stream is not null)
                {
                    return new ApiResponseData(response.Stream);
                }
                else if (response.Model is not null)
                {
                    var bytes = serializer.SerializeBytes(response.Model);
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
                    var result = await DispatchWithResult(bus, data, cancellationToken);
                    var bytes = serializer.SerializeBytes(result);
                    return new ApiResponseData(bytes);
                }
                else
                {
                    await Dispatch(bus, data, cancellationToken);
                    return new ApiResponseData();
                }
            }

            return null;
        }

        private static Task<RemoteQueryCallResponse> Call(IBus bus, ISerializer serializer, ApiRequestData data, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(data.ProviderType)) throw new ArgumentNullException(nameof(ApiRequestData.ProviderType));
            if (String.IsNullOrWhiteSpace(data.ProviderMethod)) throw new ArgumentNullException(nameof(ApiRequestData.ProviderMethod));
            if (data.ProviderArguments is null) throw new ArgumentNullException(nameof(ApiRequestData.ProviderArguments));
            if (String.IsNullOrWhiteSpace(data.Source)) throw new ArgumentNullException(nameof(ApiRequestData.Source));

            var providerType = TypeFinder.GetTypeFromName(data.ProviderType);
            if (!providerType.IsInterface)
                throw new ArgumentException($"Provider {data.ProviderType} is not an interface type");
            var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

            return bus.RemoteHandleQueryCallAsync(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, true, serializer, cancellationToken);
        }

        private static Task Dispatch(IBus bus, ApiRequestData data, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(data.MessageType)) throw new ArgumentNullException(nameof(ApiRequestData.MessageType));
            if (data.MessageData is null) throw new ArgumentNullException(nameof(ApiRequestData.MessageData));
            if (String.IsNullOrWhiteSpace(data.Source)) throw new ArgumentNullException(nameof(ApiRequestData.Source));

            var commandType = TypeFinder.GetTypeFromName(data.MessageType);
            var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);
            if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                throw new Exception($"Type {data.MessageType} is not a command");

            var command = (ICommand?)JsonSerializer.Deserialize(data.MessageData, commandType);
            if (command is null)
                throw new Exception($"Invalid {nameof(data.MessageData)}");

            if (data.MessageAwait)
                return bus.RemoteHandleCommandDispatchAwaitAsync(command, data.Source, true, cancellationToken);
            else
                return bus.RemoteHandleCommandDispatchAsync(command, data.Source, true, cancellationToken);
        }
        private static Task<object?> DispatchWithResult(IBus bus, ApiRequestData data, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(data.MessageType)) throw new ArgumentNullException(nameof(ApiRequestData.MessageType));
            if (data.MessageData is null) throw new ArgumentNullException(nameof(ApiRequestData.MessageData));
            if (String.IsNullOrWhiteSpace(data.Source)) throw new ArgumentNullException(nameof(ApiRequestData.Source));

            var commandType = TypeFinder.GetTypeFromName(data.MessageType);
            var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);
            if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                throw new Exception($"Type {data.MessageType} is not a command");

            var command = (ICommand?)JsonSerializer.Deserialize(data.MessageData, commandType);
            if (command is null)
                throw new Exception($"Invalid {nameof(data.MessageData)}");

            return bus.RemoteHandleCommandWithResultDispatchAwaitAsync(command, data.Source, true, cancellationToken);
        }
    }
}