using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Providers.Options;

public sealed class AirtelMoneyOptions
{
    public const string SectionName = "Providers:AirtelMoney";

    [Required(ErrorMessage = "Airtel ClientId is required.")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Airtel ClientSecret is required.")]
    public string ClientSecret { get; set; } = string.Empty;

    public string Environment { get; set; } = "Sandbox";
    public string BaseUrl { get; set; } = "https://openapiuat.airtel.africa";
    public string CallbackUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
