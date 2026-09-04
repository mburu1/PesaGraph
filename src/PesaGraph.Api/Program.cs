using System;
using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PesaGraph.Audit.DependencyInjection;
using PesaGraph.Conversational.DependencyInjection;
using PesaGraph.Infrastructure.DependencyInjection;
using PesaGraph.Ingestion.DependencyInjection;
using PesaGraph.Ledger.DependencyInjection;
using PesaGraph.Liquidity.DependencyInjection;
using PesaGraph.Notifications.DependencyInjection;
using PesaGraph.Providers.DependencyInjection;
using PesaGraph.Reconciliation.DependencyInjection;
using PesaGraph.Shared.DependencyInjection;
using PesaGraph.Tenancy.DependencyInjection;
using Scalar.AspNetCore;
using Serilog;

// Load .env variables into environment if present
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console();
});

// 1. Shared Kernel & Tenancy Context
builder.Services.AddSharedKernel();

// 2. Infrastructure (PostgreSQL EF Core, MongoDb, Redis, MassTransit/RabbitMQ)
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Provider Adapters (Daraja, Airtel Money, WhatsApp Cloud API, SMS)
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

// 5. API & OpenAPI / Scalar
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure Middleware Pipeline
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("PesaGraph API — Operations Brain")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

namespace PesaGraph.Api
{
    public partial class Program { }
}
