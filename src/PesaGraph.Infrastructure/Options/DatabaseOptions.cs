using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(ErrorMessage = "PostgreSQL connection string is required.")]
    public string ConnectionString { get; set; } = string.Empty;

    public int MaxRetryCount { get; set; } = 3;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; } = false;
}
