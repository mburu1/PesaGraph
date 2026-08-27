using System;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PesaGraph.Audit.DependencyInjection;
using PesaGraph.Conversational.DependencyInjection;
using PesaGraph.Infrastructure.DependencyInjection;
using PesaGraph.Infrastructure.Options;
using PesaGraph.Ingestion.DependencyInjection;
using PesaGraph.Ledger.DependencyInjection;
using PesaGraph.Liquidity.DependencyInjection;
using PesaGraph.Notifications.DependencyInjection;
using PesaGraph.Providers.DependencyInjection;
using PesaGraph.Reconciliation.DependencyInjection;
using PesaGraph.Shared.DependencyInjection;
using PesaGraph.Tenancy.DependencyInjection;
using PesaGraph.Workers.Consumers;
using PesaGraph.Workers.Jobs;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
builder.Services.AddSerilog(loggerConfig =>
{
    loggerConfig
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console();
});

// 1. Shared Kernel
builder.Services.AddSharedKernel();

// 2. Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Provider Adapters
builder.Services.AddProviderAdapters(builder.Configuration);

// 4. Domain Modules
builder.Services
    .AddTenancyModule()
    .AddIngestionModule()
    .AddLedgerModule()
    .AddReconciliationModule()
    .AddLiquidityModule()
    .AddConversationalModule()
    .AddNotificationsModule()
    .AddAuditModule();

// 5. MassTransit with Consumers
builder.Services.AddMassTransit(busConfig =>
{
    busConfig.SetKebabCaseEndpointNameFormatter();
    busConfig.AddConsumer<CanonicalTransactionConsumer>();

    busConfig.UsingRabbitMq((context, cfg) =>
    {
        var rabbitOpts = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        cfg.Host(rabbitOpts.Host, rabbitOpts.Port, rabbitOpts.VirtualHost, h =>
        {
            h.Username(rabbitOpts.Username);
            h.Password(rabbitOpts.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// 6. Background Hosted Services
builder.Services.AddHostedService<DailyFloatDigestWorker>();

var host = builder.Build();
await host.RunAsync();
