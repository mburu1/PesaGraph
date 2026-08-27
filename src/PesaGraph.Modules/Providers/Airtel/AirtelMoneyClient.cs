using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PesaGraph.Providers.Airtel.Models;
using PesaGraph.Providers.Options;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Providers.Airtel;

public interface IAirtelMoneyClient
{
    Task<Result<string>> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public class AirtelMoneyClient : IAirtelMoneyClient
{
    private readonly HttpClient _httpClient;
    private readonly AirtelMoneyOptions _options;

    public AirtelMoneyClient(HttpClient httpClient, IOptions<AirtelMoneyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Result<string>> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                client_id = _options.ClientId,
                client_secret = _options.ClientSecret,
                grant_type = "client_credentials"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/auth/oauth2/token");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<string>(Error.Failure("Airtel.AuthFailed", $"Failed to authenticate with Airtel: {content}"));
            }

            var authData = JsonSerializer.Deserialize<AirtelAuthResponse>(content);
            return authData != null && !string.IsNullOrWhiteSpace(authData.AccessToken)
                ? Result.Success(authData.AccessToken)
                : Result.Failure<string>(Error.Failure("Airtel.TokenInvalid", "Empty token returned by Airtel."));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(Error.Failure("Airtel.Exception", ex.Message));
        }
    }
}
