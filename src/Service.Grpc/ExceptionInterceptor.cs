using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Service.Grpc
{
	public class ExceptionInterceptor : Interceptor
	{
		private readonly ILogger _logger;

		public ExceptionInterceptor(ILogger logger) => _logger = logger;

		public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
		{
			var sourceAppName = "-none-";
			var sourceAppVersion = "-none-";
			var sourceAppHost = "-none-";
			try
			{
				sourceAppName = context.RequestHeaders?.Get(CallSourceInterceptor.GrpcSourceAppHostHeader)?.Value;
				sourceAppVersion = context.RequestHeaders?.Get(CallSourceInterceptor.GrpcSourceAppVersionHeader)?.Value;
				sourceAppHost = context.RequestHeaders?.Get(CallSourceInterceptor.GrpcSourceAppHostHeader)?.Value;

				return await base.UnaryServerHandler(request, context, continuation);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GRPC service exception. Path: {path}; Request: {requestJson}; Source: {sourceAppName}.{sourceAppVersion} [{sourceHost}]",
					context.Method, JsonSerializer.Serialize(request), sourceAppName, sourceAppVersion, sourceAppHost);

				throw;
			}
		}
	}
}