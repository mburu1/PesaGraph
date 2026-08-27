using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PesaGraph.Providers.Airtel;
using PesaGraph.Providers.Options;
using Xunit;

namespace PesaGraph.UnitTests.Providers;

public class AirtelMoneyClientTests
{
    private readonly IOptions<AirtelMoneyOptions> _options;

    public AirtelMoneyClientTests()
    {
        _options = Options.Create(new AirtelMoneyOptions
        {
            BaseUrl = "https://sandbox-openapi.airtel.africa",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        });
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"access_token\":\"airtel-token-xyz\",\"expires_in\":3600,\"token_type\":\"Bearer\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new AirtelMoneyClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("airtel-token-xyz");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithInvalidCredentials_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\":\"Invalid client credentials\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new AirtelMoneyClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Airtel.AuthFailed");
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
        var sut = new AirtelMoneyClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Airtel.TokenInvalid");
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
        var sut = new AirtelMoneyClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Airtel.TokenInvalid");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenServerReturnsError_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("{\"error\":\"Internal server error\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new AirtelMoneyClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Airtel.AuthFailed");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithHttpException_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Network unreachable"));
        var httpClient = new HttpClient(handler);
        var sut = new AirtelMoneyClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Airtel.Exception");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithTimeoutException_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(new OperationCanceledException("Request timeout"));
        var httpClient = new HttpClient(handler);
        var sut = new AirtelMoneyClient(httpClient, _options);

        var result = await sut.GetAccessTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Airtel.Exception");
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        private readonly Exception? _exception;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public MockHttpMessageHandler(Exception exception)
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
}
