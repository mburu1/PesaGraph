using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required(ErrorMessage = "RabbitMQ Host is required.")]
    public string Host { get; set; } = "localhost";

    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";

    [Required(ErrorMessage = "RabbitMQ Username is required.")]
    public string Username { get; set; } = "guest";

    [Required(ErrorMessage = "RabbitMQ Password is required.")]
    public string Password { get; set; } = "guest";

    public int RetryLimit { get; set; } = 5;
    public int InitialIntervalMs { get; set; } = 200;
    public int IntervalIncrementMs { get; set; } = 200;
}
