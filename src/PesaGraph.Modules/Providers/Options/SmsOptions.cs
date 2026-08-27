using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Providers.Options;

public sealed class SmsOptions
{
    public const string SectionName = "Providers:Sms";

    [Required(ErrorMessage = "SMS Provider Name is required (e.g., AfricaIsTalking).")]
    public string Provider { get; set; } = "AfricasTalking";

    [Required(ErrorMessage = "SMS API Key is required.")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "SMS Username / SenderId is required.")]
    public string Username { get; set; } = "sandbox";

    public string SenderId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.africastalking.com/version1";
}
