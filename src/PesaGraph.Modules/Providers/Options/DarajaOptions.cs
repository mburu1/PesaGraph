using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Providers.Options;

public sealed class DarajaOptions
{
    public const string SectionName = "Providers:Daraja";

    [Required(ErrorMessage = "Daraja ConsumerKey is required.")]
    public string ConsumerKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Daraja ConsumerSecret is required.")]
    public string ConsumerSecret { get; set; } = string.Empty;

    [Required(ErrorMessage = "Daraja PassKey is required.")]
    public string PassKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Daraja ShortCode is required.")]
    public string ShortCode { get; set; } = string.Empty;

    public string Environment { get; set; } = "Sandbox"; // Sandbox or Production
    public string BaseUrl { get; set; } = "https://sandbox.safaricom.co.ke";
    public string CallbackUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
