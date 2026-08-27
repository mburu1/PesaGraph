using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PesaGraph.Providers.Options;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Providers.Sms;

public interface ISmsClient
{
    Task<Result<string>> SendSmsAsync(string toPhone, string message, CancellationToken cancellationToken = default);
}

public class SmsClient : ISmsClient
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;

    public SmsClient(HttpClient httpClient, IOptions<SmsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Result<string>> SendSmsAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", _options.Username),
                new KeyValuePair<string, string>("to", toPhone),
                new KeyValuePair<string, string>("message", message),
                new KeyValuePair<string, string>("from", _options.SenderId)
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/messaging");
            request.Headers.Add("apiKey", _options.ApiKey);
            request.Headers.Add("Accept", "application/json");
            request.Content = content;

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return response.IsSuccessStatusCode
                ? Result.Success(responseBody)
                : Result.Failure<string>(Error.Failure("Sms.SendFailed", responseBody));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(Error.Failure("Sms.Exception", ex.Message));
        }
    }
}
