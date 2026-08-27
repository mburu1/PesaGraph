using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PesaGraph.Providers.Daraja.Models;
using PesaGraph.Providers.Options;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Providers.Daraja;

public interface IDarajaClient
{
    Task<Result<string>> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> InitiateStkPushAsync(string phone, decimal amount, string accountReference, string transactionDesc, CancellationToken cancellationToken = default);
}

public class DarajaClient : IDarajaClient
{
    private readonly HttpClient _httpClient;
    private readonly DarajaOptions _options;

    public DarajaClient(HttpClient httpClient, IOptions<DarajaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Result<string>> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ConsumerKey}:{_options.ConsumerSecret}"));
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/oauth/v1/generate?grant_type=client_credentials");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure<string>(Error.Failure("Daraja.AuthFailed", $"Failed to authenticate with Daraja: {errorBody}"));
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var authData = JsonSerializer.Deserialize<DarajaAuthResponse>(content);

            return authData != null && !string.IsNullOrWhiteSpace(authData.AccessToken)
                ? Result.Success(authData.AccessToken)
                : Result.Failure<string>(Error.Failure("Daraja.TokenInvalid", "Empty token returned by Daraja."));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(Error.Failure("Daraja.Exception", ex.Message));
        }
    }

    public async Task<Result<string>> InitiateStkPushAsync(string phone, decimal amount, string accountReference, string transactionDesc, CancellationToken cancellationToken = default)
    {
        var tokenResult = await GetAccessTokenAsync(cancellationToken);
        if (tokenResult.IsFailure) return Result.Failure<string>(tokenResult.Error);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ShortCode}{_options.PassKey}{timestamp}"));

        var payload = new
        {
            BusinessShortCode = _options.ShortCode,
            Password = password,
            Timestamp = timestamp,
            TransactionType = "CustomerPayBillOnline",
            Amount = (int)Math.Round(amount, MidpointRounding.AwayFromZero),
            PartyA = phone,
            PartyB = _options.ShortCode,
            PhoneNumber = phone,
            CallBackURL = _options.CallbackUrl,
            AccountReference = accountReference,
            TransactionDesc = transactionDesc
        };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/mpesa/stkpush/v1/processrequest");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return response.IsSuccessStatusCode
                ? Result.Success(responseBody)
                : Result.Failure<string>(Error.Failure("Daraja.StkFailed", responseBody));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(Error.Failure("Daraja.Exception", ex.Message));
        }
    }
}
