using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PesaGraph.Providers.Daraja;
using PesaGraph.Providers.Options;
using Xunit;

namespace PesaGraph.UnitTests.Providers;

public class DarajaClientTests
{
    private readonly IOptions<DarajaOptions> _options;

    public DarajaClientTests()
    {
        _options = Options.Create(new DarajaOptions
        {
            BaseUrl = "https://sandbox.safaricom.co.ke",
            ConsumerKey = "test-key",
            ConsumerSecret = "test-secret",
            ShortCode = "174379",
            PassKey = "bfb279f9aa9bdbcf158e97dd1a2c2f2f9e6c2089332d7c22dd77330dba3f66",
            CallbackUrl = "https://example.com/callback"
        });
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"access_token\":\"test-token-123\",\"expires_in\":3600}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test-token-123");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithInvalidCredentials_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\":\"Invalid credentials\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Daraja.AuthFailed");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenEmptyTokenReturned_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"access_token\":\"\",\"expires_in\":3600}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Daraja.TokenInvalid");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithNullToken_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"access_token\":null,\"expires_in\":3600}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Daraja.TokenInvalid");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithHttpException_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Connection timeout"));
        var httpClient = new HttpClient(handler);
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Daraja.Exception");
    }

    [Fact]
    public async Task InitiateStkPushAsync_WithValidCredentials_ReturnsSuccess()
    {
        var handlers = new[]
        {
            new MockHttpMessageHandler(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"valid-token\",\"expires_in\":3600}")
                }),
            new MockHttpMessageHandler(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"ResponseCode\":\"0\",\"ResponseDescription\":\"Success\"}")
                })
        };

        var httpClient = new HttpClient(new SequentialHttpMessageHandler(handlers));
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.InitiateStkPushAsync(
            "254712345678",
            1000,
            "INV-001",
            "Payment for services");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task InitiateStkPushAsync_WhenTokenAuthFails_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\":\"Invalid credentials\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.InitiateStkPushAsync(
            "254712345678",
            1000,
            "INV-001",
            "Payment for services");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Daraja.AuthFailed");
    }

    [Fact]
    public async Task InitiateStkPushAsync_WithStkRequestFailure_ReturnsFailure()
    {
        var handlers = new[]
        {
            new MockHttpMessageHandler(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"access_token\":\"valid-token\",\"expires_in\":3600}")
                }),
            new MockHttpMessageHandler(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\":\"Invalid amount\"}")
                })
        };

        var httpClient = new HttpClient(new SequentialHttpMessageHandler(handlers));
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.InitiateStkPushAsync(
            "254712345678",
            -100,
            "INV-001",
            "Payment for services");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Daraja.StkFailed");
    }

    [Fact]
    public async Task InitiateStkPushAsync_WithException_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(handler);
        var sut = new DarajaClient(httpClient, _options);

        var result = await sut.InitiateStkPushAsync(
            "254712345678",
            1000,
            "INV-001",
            "Payment for services");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Daraja.Exception");
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        private readonly HttpRequestException? _exception;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public MockHttpMessageHandler(HttpRequestException exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_exception != null)
            {
                return Task.FromException<HttpResponseMessage>(_exception);
            }

            return Task.FromResult(_response);
        }
    }

    private class SequentialHttpMessageHandler : HttpMessageHandler
    {
        private readonly MockHttpMessageHandler[] _handlers;
        private int _callCount = 0;

        public SequentialHttpMessageHandler(params MockHttpMessageHandler[] handlers)
        {
            _handlers = handlers;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_callCount >= _handlers.Length)
            {
                throw new InvalidOperationException("Unexpected HTTP call count exceeded.");
            }

            var handler = _handlers[_callCount++];
            var baseMethod = typeof(HttpMessageHandler).GetMethod("SendAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (baseMethod == null)
            {
                throw new InvalidOperationException("Could not find SendAsync method.");
            }

            var result = baseMethod.Invoke(handler, new object[] { request, cancellationToken });
            if (result is Task<HttpResponseMessage> task)
            {
                return await task;
            }

            throw new InvalidOperationException("Failed to invoke SendAsync");
        }
    }
}
