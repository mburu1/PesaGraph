using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Infrastructure.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; } = false;

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "pesagraph-consumer-group";
    public string TransactionEventsTopic { get; set; } = "pesagraph.canonical.transactions";
}
