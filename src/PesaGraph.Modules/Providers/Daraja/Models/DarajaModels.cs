using System;
using System.Text.Json.Serialization;

namespace PesaGraph.Providers.Daraja.Models;

public record DarajaAuthResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] object? ExpiresIn);

public record DarajaStkCallback(
    [property: JsonPropertyName("Body")] DarajaStkBody Body);

public record DarajaStkBody(
    [property: JsonPropertyName("stkCallback")] DarajaStkCallbackData StkCallback);

public record DarajaStkCallbackData(
    [property: JsonPropertyName("MerchantRequestID")] string MerchantRequestId,
    [property: JsonPropertyName("CheckoutRequestID")] string CheckoutRequestId,
    [property: JsonPropertyName("ResultCode")] int ResultCode,
    [property: JsonPropertyName("ResultDesc")] string ResultDesc,
    [property: JsonPropertyName("CallbackMetadata")] DarajaCallbackMetadata? CallbackMetadata);

public record DarajaCallbackMetadata(
    [property: JsonPropertyName("Item")] DarajaCallbackItem[]? Items);

public record DarajaCallbackItem(
    [property: JsonPropertyName("Name")] string Name,
    [property: JsonPropertyName("Value")] object? Value);

public record DarajaC2BValidationRequest(
    [property: JsonPropertyName("TransactionType")] string TransactionType,
    [property: JsonPropertyName("TransID")] string TransId,
    [property: JsonPropertyName("TransTime")] string TransTime,
    [property: JsonPropertyName("TransAmount")] string TransAmount,
    [property: JsonPropertyName("BusinessShortCode")] string BusinessShortCode,
    [property: JsonPropertyName("BillRefNumber")] string? BillRefNumber,
    [property: JsonPropertyName("InvoiceNumber")] string? InvoiceNumber,
    [property: JsonPropertyName("OrgAccountBalance")] string? OrgAccountBalance,
    [property: JsonPropertyName("ThirdPartyTransID")] string? ThirdPartyTransId,
    [property: JsonPropertyName("MSISDN")] string Msisdn,
    [property: JsonPropertyName("FirstName")] string? FirstName,
    [property: JsonPropertyName("MiddleName")] string? MiddleName,
    [property: JsonPropertyName("LastName")] string? LastName);

public record DarajaC2BConfirmationResponse(
    [property: JsonPropertyName("ResultCode")] string ResultCode,
    [property: JsonPropertyName("ResultDesc")] string ResultDesc);

public record DarajaB2CResult(
    [property: JsonPropertyName("Result")] DarajaB2CResultData Result);

public record DarajaB2CResultData(
    [property: JsonPropertyName("ResultType")] int ResultType,
    [property: JsonPropertyName("ResultCode")] int ResultCode,
    [property: JsonPropertyName("ResultDesc")] string ResultDesc,
    [property: JsonPropertyName("OriginatorConversationID")] string OriginatorConversationId,
    [property: JsonPropertyName("ConversationID")] string ConversationId,
    [property: JsonPropertyName("TransactionID")] string TransactionId,
    [property: JsonPropertyName("ResultParameters")] DarajaResultParameters? ResultParameters);

public record DarajaResultParameters(
    [property: JsonPropertyName("ResultParameter")] DarajaCallbackItem[]? ResultParameter);
