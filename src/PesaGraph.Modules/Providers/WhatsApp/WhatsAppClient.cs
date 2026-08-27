using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PesaGraph.Providers.Options;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Providers.WhatsApp;

public interface IWhatsAppClient
{
    Task<Result<string>> SendTextMessageAsync(string toPhone, string text, CancellationToken cancellationToken = default);
}

public class WhatsAppClient : IWhatsAppClient
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;

    public WhatsAppClient(HttpClient httpClient, IOptions<WhatsAppOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Result<string>> SendTextMessageAsync(string toPhone, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = toPhone,
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = text
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/{_options.PhoneNumberId}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return response.IsSuccessStatusCode
                ? Result.Success(responseBody)
                : Result.Failure<string>(Error.Failure("WhatsApp.SendFailed", responseBody));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(Error.Failure("WhatsApp.Exception", ex.Message));
        }
    }
}
