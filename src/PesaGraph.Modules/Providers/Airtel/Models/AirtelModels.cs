using System;
using System.Text.Json.Serialization;

namespace PesaGraph.Providers.Airtel.Models;

public record AirtelAuthResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

public record AirtelCallbackPayload(
    [property: JsonPropertyName("transaction")] AirtelTransactionDetails Transaction);

public record AirtelTransactionDetails(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("status_code")] string StatusCode,
    [property: JsonPropertyName("airtel_money_id")] string AirtelMoneyId);
