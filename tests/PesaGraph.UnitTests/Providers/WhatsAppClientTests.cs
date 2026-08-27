using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PesaGraph.Providers.Options;
using PesaGraph.Providers.WhatsApp;
using Xunit;

namespace PesaGraph.UnitTests.Providers;

public class WhatsAppClientTests
{
    private readonly IOptions<WhatsAppOptions> _options;

    public WhatsAppClientTests()
    {
        _options = Options.Create(new WhatsAppOptions
        {
            BaseUrl = "https://graph.instagram.com/v18.0",
            PhoneNumberId = "123456789",
            AccessToken = "test-access-token"
        });
    }

    [Fact]
    public async Task SendTextMessageAsync_WithValidMessage_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"messages\":[{\"id\":\"wamid.123\",\"message_status\":\"accepted\"}]}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "Hello from WhatsApp");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("wamid");
    }

    [Fact]
    public async Task SendTextMessageAsync_WithInvalidPhoneNumber_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":{\"message\":\"Invalid phone number format\"}}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("invalid", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.SendFailed");
    }

    [Fact]
    public async Task SendTextMessageAsync_WithEmptyMessage_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":{\"message\":\"Message text is required\"}}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.SendFailed");
    }

    [Fact]
    public async Task SendTextMessageAsync_WithInvalidAccessToken_ReturnsForbidden()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("{\"error\":{\"message\":\"Invalid access token\"}}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.SendFailed");
    }

    [Fact]
    public async Task SendTextMessageAsync_WithUnauthorized_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\":{\"message\":\"Unauthorized\"}}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.SendFailed");
    }

    [Fact]
    public async Task SendTextMessageAsync_WhenServerError_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("{\"error\":{\"message\":\"Internal server error\"}}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.SendFailed");
    }

    [Fact]
    public async Task SendTextMessageAsync_WithHttpException_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Connection failed"));
        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.Exception");
    }

    [Fact]
    public async Task SendTextMessageAsync_WithNetworkTimeout_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Request timeout"));
        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.Exception");
    }

    [Fact]
    public async Task SendTextMessageAsync_WithRateLimitError_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("{\"error\":{\"message\":\"Rate limit exceeded\"}}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.SendFailed");
    }

    [Fact]
    public async Task SendTextMessageAsync_WithValidResponse_ExtractsMessageId()
    {
        const string expectedResponse = "{\"messages\":[{\"id\":\"wamid.VGhlIHRlc3QgbWVzc2FnZQ==\",\"message_status\":\"accepted\"}]}";
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedResponse)
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", "The test message");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task SendTextMessageAsync_WithLongMessage_ReturnsSuccess()
    {
        var longMessage = new string('a', 4096);
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"messages\":[{\"id\":\"wamid.123\"}]}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new WhatsAppClient(httpClient, _options);

        var result = await sut.SendTextMessageAsync("254712345678", longMessage);

        result.IsSuccess.Should().BeTrue();
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
