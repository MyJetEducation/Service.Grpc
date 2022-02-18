using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Service.Grpc
{
	public class CallIdServerInterceptor : Interceptor
	{
		public const string CallIdKey = "callid";

		private readonly ILogger _logger;
		private readonly IGrpcResponseCache _grpcResponseCache;

		public CallIdServerInterceptor(ILogger logger, IGrpcResponseCache grpcResponseCache)
		{
			_logger = logger;
			_grpcResponseCache = grpcResponseCache;
		}

		public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
		{
			Task<TResponse> GetNewResponse() => base.UnaryServerHandler(request, context, continuation);

			if (!Guid.TryParse(context.RequestHeaders.Get(CallIdKey)?.Value, out Guid callId))
				return await GetNewResponse();

			_logger.LogDebug("Process response for request: {requestJson}, server_host: {host}, client_host: {clinet}, method: {method}, callId: {callid}", JsonSerializer.Serialize(request), context.Host, context.Peer, context.Method, callId);

			var cachedResponse = _grpcResponseCache.Get<TResponse>(callId);
			if (cachedResponse != null)
			{
				_logger.LogDebug("Retrieved existing response {response} for callId: {callid}.", JsonSerializer.Serialize(cachedResponse), callId);

				return cachedResponse;
			}

			TResponse response = await GetNewResponse();

			_grpcResponseCache.Set(callId, response);

			_logger.LogDebug("Generated new response {response} for callId: {callid}.", JsonSerializer.Serialize(response), callId);

			return response;
		}
	}
}