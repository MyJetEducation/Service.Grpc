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

		public CallIdServerInterceptor(ILogger logger) => _logger = logger;

		public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
		{
			string callId = context.RequestHeaders.Get(CallIdKey)?.Value;

			_logger.LogDebug("Process response for request: {requestJson}, server_host: {host}, client_host: {clinet}, method: {method}, callId: {callid}", JsonSerializer.Serialize(request), context.Host, context.Peer, context.Method, callId);

			return await base.UnaryServerHandler(request, context, continuation);
		}
	}
}