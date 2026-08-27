using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Providers.Options;

public sealed class WhatsAppOptions
{
    public const string SectionName = "Providers:WhatsApp";

    [Required(ErrorMessage = "WhatsApp Access Token is required.")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "WhatsApp Phone Number ID is required.")]
    public string PhoneNumberId { get; set; } = string.Empty;

    public string BusinessAccountId { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://graph.facebook.com/v21.0";
    public int TimeoutSeconds { get; set; } = 20;
}
