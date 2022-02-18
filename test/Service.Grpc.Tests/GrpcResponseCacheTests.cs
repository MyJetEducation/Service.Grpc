using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Service.Core.Client.Services;

namespace Service.Grpc.Tests
{
	public class GrpcResponseCacheTests
	{
		private GrpcResponseCache _sut;
		private Mock<ISystemClock> _systemClock;

		[SetUp]
		public void Setup()
		{
			_systemClock = new Mock<ISystemClock>();
			_sut = new GrpcResponseCache(_systemClock.Object, new Mock<ILogger<GrpcResponseCache>>().Object, 200, 20);
		}

		[Test]
		public void Get_return_null_if_nothing_cached_by_call_id()
		{
			Guid callId = new("ad312633-cf90-4b06-b2f2-416f431034b0");

			var result = _sut.Get<TestDto>(callId);

			Assert.IsNull(result);
		}

		[Test]
		public void Get_return_dto_if_is_cached_by_call_id()
		{
			Guid callId = new("69835d8a-bf6f-4b3a-8d77-93bd34634cd8");

			var testDto = new TestDto();

			_sut.Set(callId, testDto);

			var result = _sut.Get<TestDto>(callId);

			Assert.AreSame(result, testDto);
		}

		[Test]
		public void Get_return_null_after_clear_cache()
		{
			Guid callId = new("26e8a313-0707-4051-8784-4054fa0e12e9");

			var testDto = new TestDto();

			_systemClock
				.Setup(clock => clock.Now)
				.Returns(() => DateTime.UtcNow);

			_sut.Set(callId, testDto);

			Thread.Sleep(1000);

			var result = _sut.Get<TestDto>(callId);

			Assert.IsNull(result);
		}

		[Test]
		public void Test_parallel()
		{
			Parallel.For(1, 30000, (_, _) =>
			{
				var callId = Guid.NewGuid();

				var testDto = new TestDto();

				_sut.Set(callId, testDto);

				var result = _sut.Get<TestDto>(callId);

				Assert.AreSame(result, testDto);
			});
		}


		private class TestDto
		{
		}
	}
}