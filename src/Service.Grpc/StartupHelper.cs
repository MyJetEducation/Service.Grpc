using System;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.GrpcMetrics;
using ProtoBuf.Grpc.Server;
using Service.Core.Client.Services;

namespace Service.Grpc
{
	public static class StartupHelper
	{
		public static IServiceCollection BindGrpc(this IServiceCollection services, Action<GrpcServiceOptions>? configureOptions = null, int grpcResponseCacheTimeout = 3000)
		{
			services.AddSingleton<ILogger>(svc => svc.GetRequiredService<ILogger<GrpcClientFactory>>());

			services.AddCodeFirstGrpc(options =>
			{
				options.Interceptors.Add<PrometheusMetricsInterceptor>();
				options.Interceptors.Add<CallSourceInterceptor>();
				options.Interceptors.Add<CallIdServerInterceptor>();
				options.Interceptors.Add<ExceptionInterceptor>();

				configureOptions?.Invoke(options);
			});

			ServiceProvider serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetService<ILogger<GrpcResponseCache>>();

			services.AddSingleton<IGrpcResponseCache>(_ => new GrpcResponseCache(new SystemClock(), logger, grpcResponseCacheTimeout));

			return services;
		}
	}
}