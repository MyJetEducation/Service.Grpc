using System;
using Microsoft.Extensions.Logging;

namespace Service.Grpc
{
	public class GrpcClientFactory
	{
		private readonly string _grpcServiceUrl;
		private readonly ILogger _logger;

		public GrpcClientFactory(string grpcServiceUrl, ILogger logger)
		{
			_grpcServiceUrl = grpcServiceUrl;
			_logger = logger;

			AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
		}

		public GrpcServiceProxy<TService> CreateGrpcService<TService>() where TService : class => new(_grpcServiceUrl, _logger);
	}
}