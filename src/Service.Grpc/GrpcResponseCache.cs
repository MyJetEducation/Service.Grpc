using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using Service.Core.Client.Services;

namespace Service.Grpc
{
	public class GrpcResponseCache : IGrpcResponseCache
	{
		private readonly ISystemClock _systemClock;
		private readonly ILogger<GrpcResponseCache> _logger;

		private static readonly ConcurrentDictionary<Guid, (DateTime created, object resonse)> Dictionary;
		private readonly int _grpcResponseCacheTimeout;
		private readonly MyTaskTimer _myTaskTimer;
		private long _timerStarted;

		static GrpcResponseCache() => Dictionary = new ConcurrentDictionary<Guid, (DateTime created, object resonse)>();

		public GrpcResponseCache(ISystemClock systemClock, ILogger<GrpcResponseCache> logger, int grpcResponseCacheTimeout, int checkExpiredTimeout = 5000)
		{
			_grpcResponseCacheTimeout = grpcResponseCacheTimeout;
			_systemClock = systemClock;
			_logger = logger;

			_myTaskTimer = new MyTaskTimer(typeof(GrpcResponseCache), TimeSpan.FromMilliseconds(checkExpiredTimeout), logger, CheckHash);
		}

		public TResponse Get<TResponse>(Guid callId) where TResponse : class
		{
			return Dictionary.TryGetValue(callId, out (DateTime created, object response) value)
				? value.response as TResponse
				: null;
		}

		public void Set<TResponse>(Guid callId, TResponse response) where TResponse : class
		{
			StartTimer();

			_logger.LogDebug("Cached new response {response} with callId: {callid}.", JsonSerializer.Serialize(response), callId);

			Dictionary.AddOrUpdate(callId, (_systemClock.Now, response), (_, data) =>
			{
				data.created = _systemClock.Now;
				data.resonse = data;
				return data;
			});
		}

		public bool TimerStarted
		{
			get => Interlocked.Read(ref _timerStarted) == 1;
			set => Interlocked.Exchange(ref _timerStarted, Convert.ToInt64(value));
		}

		private void StartTimer()
		{
			if (TimerStarted)
				return;

			TimerStarted = true;

			_myTaskTimer.Start();
		}

		private Task CheckHash()
		{
			KeyValuePair<Guid, (DateTime created, object resonse)>[] pairs = Dictionary
				.Where(pair => pair.Value.created.AddMilliseconds(_grpcResponseCacheTimeout) < _systemClock.Now)
				.ToArray();

			if (pairs.Any())
			{
				foreach (KeyValuePair<Guid, (DateTime created, object resonse)> pair in pairs)
					Dictionary.TryRemove(pair);

				_logger.LogDebug("Removed {count} expired cached responses.", pairs.Length);
			}

			return Task.CompletedTask;
		}
	}
}