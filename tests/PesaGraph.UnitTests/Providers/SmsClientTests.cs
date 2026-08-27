using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PesaGraph.Providers.Options;
using PesaGraph.Providers.Sms;
using Xunit;

namespace PesaGraph.UnitTests.Providers;

public class SmsClientTests
{
    private readonly IOptions<SmsOptions> _options;

    public SmsClientTests()
    {
        _options = Options.Create(new SmsOptions
        {
            BaseUrl = "https://api.sms-provider.com",
            Username = "test-user",
            ApiKey = "test-api-key",
            SenderId = "PesaGraph"
        });
    }

    [Fact]
    public async Task SendSmsAsync_WithValidMessage_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"sent\",\"messageId\":\"msg-123\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("254712345678", "Hello, this is a test message");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("sent");
    }

    [Fact]
    public async Task SendSmsAsync_WithInvalidPhoneNumber_ReturnsBadRequest()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Invalid phone number\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("invalid-phone", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.SendFailed");
    }

    [Fact]
    public async Task SendSmsAsync_WithEmptyMessage_ReturnsValidationError()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Message cannot be empty\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("254712345678", "");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.SendFailed");
    }

    [Fact]
    public async Task SendSmsAsync_WithAuthenticationFailure_ReturnsForbidden()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("{\"error\":\"Invalid API key\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.SendFailed");
    }

    [Fact]
    public async Task SendSmsAsync_WhenServerError_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("{\"error\":\"Internal server error\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.SendFailed");
    }

    [Fact]
    public async Task SendSmsAsync_WithHttpException_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Connection failed"));
        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.Exception");
    }

    [Fact]
    public async Task SendSmsAsync_WithNetworkTimeout_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Request timeout"));
        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.Exception");
    }

    [Fact]
    public async Task SendSmsAsync_WithRateLimitError_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("{\"error\":\"Rate limit exceeded\"}")
            });

        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("254712345678", "Test message");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sms.SendFailed");
    }

    [Fact]
    public async Task SendSmsAsync_WithSuccessStatusCode_ReturnsSuccessWithResponseBody()
    {
        const string expectedResponse = "{\"status\":\"delivered\",\"messageId\":\"msg-456\",\"timestamp\":\"2024-01-01T12:00:00Z\"}";
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(expectedResponse)
            });

        var httpClient = new HttpClient(handler);
        var sut = new SmsClient(httpClient, _options);

        var result = await sut.SendSmsAsync("254712345678", "Test message");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
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
