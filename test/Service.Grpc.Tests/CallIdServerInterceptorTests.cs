using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Service.Grpc.Tests
{
	public class CallIdServerInterceptorTests
	{
		private readonly Guid _callId = new("b90aa6df-98d7-4ab9-bc45-0001e3c1af88");

		private Mock<IGrpcResponseCache> _grpcResponseCache;
		private CallIdServerInterceptor _sut;

		[SetUp]
		public void Setup()
		{
			_grpcResponseCache = new Mock<IGrpcResponseCache>();
			_sut = new CallIdServerInterceptor(new Mock<ILogger>().Object, _grpcResponseCache.Object);
		}

		[Test]
		public void UnaryServerHandler_return_default_response_if_no_call_id_found_in_headers()
		{
			ServerCallContext context = GetTestContext();
			var request = new TestRequestDto();

			Task<TestRequestDto> response = _sut.UnaryServerHandler(request, context, (_, _) => Task.FromResult(request));

			_grpcResponseCache.Verify(cache => cache.Get<TestRequestDto>(It.IsAny<Guid>()), Times.Never);
			_grpcResponseCache.Verify(cache => cache.Set(It.IsAny<Guid>(), It.IsAny<TestRequestDto>()), Times.Never);

			Assert.IsNotNull(response);
			Assert.AreEqual(response.Result, request);
		}

		[Test]
		public void UnaryServerHandler_return_default_response_if_call_id_found_and_cache_it()
		{
			ServerCallContext context = GetTestContext(new Metadata { { CallIdServerInterceptor.CallIdKey, _callId.ToString() } });
			var request = new TestRequestDto();

			_grpcResponseCache
				.Setup(cache => cache.Get<TestRequestDto>(_callId))
				.Returns((TestRequestDto)null);

			Task<TestRequestDto> response = _sut.UnaryServerHandler(request, context, (_, _) => Task.FromResult(request));

			_grpcResponseCache.Verify(cache => cache.Get<TestRequestDto>(_callId), Times.Once);

			TestRequestDto responseResult = response.Result;
			_grpcResponseCache.Verify(cache => cache.Set(_callId, responseResult), Times.Once);

			Assert.IsNotNull(response);
			Assert.AreEqual(responseResult, request);
		}

		[Test]
		public void UnaryServerHandler_return_cached_response_if_call_id_found_and_cache_contains_response()
		{
			ServerCallContext context = GetTestContext(new Metadata { { CallIdServerInterceptor.CallIdKey, _callId.ToString() } });

			var cachedDto = new TestRequestDto();
			_grpcResponseCache
				.Setup(cache => cache.Get<TestRequestDto>(_callId))
				.Returns(cachedDto);

			Task<TestRequestDto> response = _sut.UnaryServerHandler(new TestRequestDto(), context, (_, _) => Task.FromResult(new TestRequestDto()));

			TestRequestDto responseResult = response.Result;
			_grpcResponseCache.Verify(cache => cache.Set(It.IsAny<Guid>(), It.IsAny<TestRequestDto>()), Times.Never);

			Assert.IsNotNull(response);
			Assert.AreEqual(responseResult, cachedDto);
		}

		private static ServerCallContext GetTestContext(Metadata requestHeaders = null) => TestServerCallContext.Create(
			"method", "localhost", DateTime.Now.AddMinutes(30), requestHeaders ?? new Metadata()
			, CancellationToken.None, "localhost", null, null, _ => Task.CompletedTask
			, () => new WriteOptions(), _ => { });

		private class TestRequestDto
		{
		}
	}
}