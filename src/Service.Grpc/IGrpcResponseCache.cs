using System;

namespace Service.Grpc
{
	public interface IGrpcResponseCache
	{
		TResponse Get<TResponse>(Guid callId) where TResponse : class;

		void Set<TResponse>(Guid callId, TResponse response) where TResponse : class;
	}
}