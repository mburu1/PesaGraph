using System;
using System.Text.Json.Serialization;

namespace PesaGraph.Providers.WhatsApp.Models;

public record WhatsAppWebhookPayload(
    [property: JsonPropertyName("object")] string Object,
    [property: JsonPropertyName("entry")] WhatsAppEntry[]? Entry);

public record WhatsAppEntry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("changes")] WhatsAppChange[]? Changes);

public record WhatsAppChange(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("value")] WhatsAppChangeValue? Value);

public record WhatsAppChangeValue(
    [property: JsonPropertyName("messaging_product")] string MessagingProduct,
    [property: JsonPropertyName("metadata")] WhatsAppMetadata? Metadata,
    [property: JsonPropertyName("contacts")] WhatsAppContact[]? Contacts,
    [property: JsonPropertyName("messages")] WhatsAppMessage[]? Messages);

public record WhatsAppMetadata(
    [property: JsonPropertyName("display_phone_number")] string DisplayPhoneNumber,
    [property: JsonPropertyName("phone_number_id")] string PhoneNumberId);

public record WhatsAppContact(
    [property: JsonPropertyName("profile")] WhatsAppProfile? Profile,
    [property: JsonPropertyName("wa_id")] string WaId);

public record WhatsAppProfile(
    [property: JsonPropertyName("name")] string Name);

public record WhatsAppMessage(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] WhatsAppTextContent? Text);

public record WhatsAppTextContent(
    [property: JsonPropertyName("body")] string Body);
