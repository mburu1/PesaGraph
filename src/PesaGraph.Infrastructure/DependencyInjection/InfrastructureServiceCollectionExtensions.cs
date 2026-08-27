using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using PesaGraph.Infrastructure.DomainEvents;
using PesaGraph.Infrastructure.Options;
using PesaGraph.Infrastructure.Persistence;
using StackExchange.Redis;

namespace PesaGraph.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Options validation and binding
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 2. Relational Database (PostgreSQL EF Core)
        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        services.AddDbContext<PesaGraphDbContext>((sp, options) =>
        {
            if (!string.IsNullOrWhiteSpace(dbOptions.ConnectionString))
            {
                options.UseNpgsql(dbOptions.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(dbOptions.MaxRetryCount);
                    npgsqlOptions.CommandTimeout(dbOptions.CommandTimeoutSeconds);
                });
            }

            if (dbOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // 3. Document Database (MongoDB)
        services.AddSingleton<IMongoClient>(sp =>
        {
            var mongoOpts = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return new MongoClient(mongoOpts.ConnectionString);
        });

        services.AddScoped<IMongoDatabase>(sp =>
        {
            var mongoOpts = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoOpts.DatabaseName);
        });

        // 4. Cache & Coordination Store (Redis)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOpts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(redisOpts.ConnectionString);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            var redisOpts = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
            options.Configuration = redisOpts.ConnectionString;
            options.InstanceName = redisOpts.InstanceName;
        });

        // 5. Message Broker (MassTransit + RabbitMQ)
        services.AddMassTransit(busConfig =>
        {
            busConfig.SetKebabCaseEndpointNameFormatter();

            busConfig.UsingRabbitMq((context, cfg) =>
            {
                var rabbitOpts = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(rabbitOpts.Host, rabbitOpts.Port, rabbitOpts.VirtualHost, h =>
                {
                    h.Username(rabbitOpts.Username);
                    h.Password(rabbitOpts.Password);
                });

                cfg.UseMessageRetry(r => r.Incremental(
                    rabbitOpts.RetryLimit,
                    TimeSpan.FromMilliseconds(rabbitOpts.InitialIntervalMs),
                    TimeSpan.FromMilliseconds(rabbitOpts.IntervalIncrementMs)));

                cfg.ConfigureEndpoints(context);
            });
        });

        // 6. Domain Events Dispatcher
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
