using System;
using System.Threading.Tasks;

namespace Service.Grpc
{
	public interface IGrpcServiceProxy<TService>
	{
		Lazy<TService> Service { get; }

		/// <summary>
		///     Circuit calling service request
		/// </summary>
		/// <typeparam name="TResponse"></typeparam>
		/// <param name="task">Task for request</param>
		/// <param name="tries">Tries count</param>
		/// <param name="timeout">Timeout between every try in milliseconds</param>
		/// <returns>Service call response</returns>
		ValueTask<TResponse> TryCall<TResponse>(Func<TService, ValueTask<TResponse>> task, int tries = 3, int timeout = 500) where TResponse : class;
	}
}