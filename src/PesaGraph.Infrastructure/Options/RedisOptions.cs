using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Infrastructure.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required(ErrorMessage = "Redis connection string is required.")]
    public string ConnectionString { get; set; } = string.Empty;

    public string InstanceName { get; set; } = "PesaGraph:";
    public int DefaultExpirationMinutes { get; set; } = 60;
}
