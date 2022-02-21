using System;
using System.Text.Json;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Service.Grpc
{
	public class CallIdClientInterceptor : Interceptor
	{
		private Guid? _callId;
		private readonly ILogger _logger;

		public CallIdClientInterceptor(ILogger logger) => _logger = logger;

		public void SetCallId(Guid? callId) => _callId = callId;

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
		{
			if (ModifyMetadata)
				context = new(context.Method, context.Host, context.Options.WithHeaders(Metadata));

			Log(request, context);

			return base.AsyncUnaryCall(request, context, continuation);
		}

		public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
		{
			if (ModifyMetadata)
				context = new(context.Method, context.Host, context.Options.WithHeaders(Metadata));

			Log(request, context);

			return base.BlockingUnaryCall(request, context, continuation);
		}

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
		{
			if (ModifyMetadata)
				context = new(context.Method, context.Host, context.Options.WithHeaders(Metadata));

			Log(request, context);

			return base.AsyncServerStreamingCall(request, context, continuation);
		}

		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
		{
			if (ModifyMetadata)
				context = new(context.Method, context.Host, context.Options.WithHeaders(Metadata));

			Log(null, context);

			return base.AsyncClientStreamingCall(context, continuation);
		}

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
		{
			if (ModifyMetadata)
				context = new(context.Method, context.Host, context.Options.WithHeaders(Metadata));

			Log(null, context);

			return base.AsyncDuplexStreamingCall(context, continuation);
		}

		private Metadata Metadata => new() {new(CallIdServerInterceptor.CallIdKey, _callId.ToString())};

		private bool ModifyMetadata => _callId != null;

		private void Log<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class =>
			_logger.LogDebug("Process request: {requestJson}, server_host: {host}, method: {method}, callId: {callid}",
				JsonSerializer.Serialize(request),
				context.Host,
				context.Method,
				context.Options.Headers?.Get(CallIdServerInterceptor.CallIdKey)?.Value);
	}
}