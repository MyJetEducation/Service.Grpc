using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.GrpcMetrics;
using ProtoBuf.Grpc.Client;

namespace Service.Grpc
{
	public class GrpcServiceProxy<TService> : IGrpcServiceProxy<TService> where TService : class
	{
		private readonly string _grpcServiceUrl;
		private readonly ILogger _logger;

		public TService Service { get; }
		private CallIdClientInterceptor _callIdClientInterceptor;

		public GrpcServiceProxy(string grpcServiceUrl, ILogger logger)
		{
			_grpcServiceUrl = grpcServiceUrl;
			_logger = logger;
			Service = GetService();
		}

		public async ValueTask<TResponse> TryCall<TResponse>(Func<TService, ValueTask<TResponse>> task, int tries = 3, int timeout = 500) where TResponse : class
		{
			_callIdClientInterceptor.SetCallId(Guid.NewGuid());

			for (var tryNumber = 1; tryNumber <= tries; tryNumber++)
			{
				try
				{
					_logger.LogDebug("Try: {from} of {to}...", tryNumber, tries);

					return await task.Invoke(Service);
				}
				catch (Exception ex)
				{
					_logger.LogWarning("Fail! Message: {message}, used {from} of {to} tries.", ex.Message, tryNumber, tries);

					if (tryNumber < tries)
						await Task.Delay(timeout);
					else
						throw;
				}
			}

			return await Task.FromResult<TResponse>(null);
		}

		private TService GetService()
		{
			GrpcChannel channel = GrpcChannel.ForAddress(_grpcServiceUrl);

			_callIdClientInterceptor = new CallIdClientInterceptor(_logger);

			CallInvoker callInvoker = channel.Intercept(new PrometheusMetricsInterceptor(), _callIdClientInterceptor);

			return callInvoker.CreateGrpcService<TService>();
		}
	}
}