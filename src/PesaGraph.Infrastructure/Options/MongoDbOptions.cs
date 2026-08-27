using System.ComponentModel.DataAnnotations;

namespace PesaGraph.Infrastructure.Options;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    [Required(ErrorMessage = "MongoDB connection string is required.")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(ErrorMessage = "MongoDB database name is required.")]
    public string DatabaseName { get; set; } = "pesagraph_raw";
}
