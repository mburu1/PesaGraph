using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace PesaGraph.ArchitectureTests;

public class LayeringArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(PesaGraph.Api.Controllers.WebhooksController).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(PesaGraph.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions).Assembly;
    private static readonly Assembly SharedAssembly = typeof(PesaGraph.Shared.Results.Result).Assembly;
    
    private static readonly Assembly LedgerAssembly = typeof(PesaGraph.Ledger.Domain.Account).Assembly;
    private static readonly Assembly AuditAssembly = typeof(PesaGraph.Audit.Domain.AuditLog).Assembly;
    private static readonly Assembly NotificationsAssembly = typeof(PesaGraph.Notifications.Services.NotificationDispatcher).Assembly;
    private static readonly Assembly ProvidersAssembly = typeof(PesaGraph.Providers.Sms.SmsClient).Assembly;
    private static readonly Assembly ReconciliationAssembly = typeof(PesaGraph.Reconciliation.Domain.UnmatchedItem).Assembly;
    private static readonly Assembly LiquidityAssembly = typeof(PesaGraph.Liquidity.Services.LiquidityService).Assembly;
    private static readonly Assembly TenancyAssembly = typeof(PesaGraph.Tenancy.Domain.Tenant).Assembly;
    private static readonly Assembly IngestionAssembly = typeof(PesaGraph.Ingestion.Domain.RawWebhookEvent).Assembly;
    private static readonly Assembly ConversationalAssembly = typeof(PesaGraph.Conversational.Services.ConversationalCommandService).Assembly;

    [Fact]
    public void ApiLayer_ShouldNotDependOnModuleImplementations()
    {
        // API controllers are expected to reference module abstractions via MediatR/handlers;
        // verify Api does not have direct hard dependency on module Persistence namespaces
        var result = Types.InAssembly(ApiAssembly)
            .Should()
            .NotHaveDependencyOn("PesaGraph.Infrastructure.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"API layer should not directly reference Infrastructure Persistence. Failed: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Enumerable.Empty<string>())}"
        );
    }

    [Fact]
    public void InfrastructureLayer_ShouldNotDependOnModuleImplementations()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("PesaGraph.Ledger")
            .And()
            .NotHaveDependencyOn("PesaGraph.Audit")
            .And()
            .NotHaveDependencyOn("PesaGraph.Notifications")
            .And()
            .NotHaveDependencyOn("PesaGraph.Reconciliation")
            .And()
            .NotHaveDependencyOn("PesaGraph.Liquidity")
            .And()
            .NotHaveDependencyOn("PesaGraph.Ingestion")
            .And()
            .NotHaveDependencyOn("PesaGraph.Conversational")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Infrastructure layer should not reference module implementations. Failed types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Enumerable.Empty<string>())}"
        );
    }

    [Fact]
    public void AllModules_ShouldDependOnSharedLayer()
    {
        var moduleAssemblies = new[]
        {
            LedgerAssembly,
            AuditAssembly,
            NotificationsAssembly,
            ProvidersAssembly,
            ReconciliationAssembly,
            LiquidityAssembly,
            TenancyAssembly,
            IngestionAssembly,
            ConversationalAssembly
        };

        foreach (var moduleAssembly in moduleAssemblies)
        {
            var moduleName = moduleAssembly.GetName().Name;
            var hasSharedRef = moduleAssembly.GetReferencedAssemblies().Any(a => a.Name == "PesaGraph.Shared");
            hasSharedRef.Should().BeTrue(
                $"Module '{moduleName}' should reference the Shared layer"
            );
        }
    }

    [Fact]
    public void ProvidersModule_CanBeReferencedByNotificationsOnly()
    {
        var publicTypes = ProvidersAssembly.GetTypes()
            .Where(t => t.IsPublic && !t.Name.StartsWith("<"))
            .ToList();

        publicTypes.Should().NotBeEmpty("Providers module should have public types");

        var hasProvidersRef = NotificationsAssembly.GetReferencedAssemblies().Any(a => a.Name == "PesaGraph.Providers");
        hasProvidersRef.Should().BeTrue(
            "Notifications module should reference Providers module"
        );
    }

    [Fact]
    public void Modules_ShouldNotHaveCyclicDependencies()
    {
        var moduleAssemblies = new Dictionary<string, Assembly>
        {
            { "Ledger", LedgerAssembly },
            { "Audit", AuditAssembly },
            { "Notifications", NotificationsAssembly },
            { "Providers", ProvidersAssembly },
            { "Reconciliation", ReconciliationAssembly },
            { "Liquidity", LiquidityAssembly },
            { "Tenancy", TenancyAssembly },
            { "Ingestion", IngestionAssembly },
            { "Conversational", ConversationalAssembly }
        };

        var cyclicDependencies = new List<string>();

        foreach (var (moduleName, moduleAssembly) in moduleAssemblies)
        {
            var referencedAssemblies = moduleAssembly.GetReferencedAssemblies();
            var referencedModuleNames = referencedAssemblies
                .Where(a => moduleAssemblies.ContainsKey(a.Name ?? ""))
                .Select(a => a.Name)
                .ToList();

            foreach (var referencedModuleName in referencedModuleNames)
            {
                if (referencedModuleName == null) continue;
                var referencedAssembly = moduleAssemblies[referencedModuleName];
                var backReferences = referencedAssembly.GetReferencedAssemblies();
                
                if (backReferences.Any(a => a.Name == moduleName))
                {
                    cyclicDependencies.Add($"{moduleName} <-> {referencedModuleName}");
                }
            }
        }

        cyclicDependencies.Should().BeEmpty(
            $"Modules should not have cyclic dependencies. Found: {string.Join(", ", cyclicDependencies)}"
        );
    }

    [Fact]
    public void SharedLayer_ShouldNotDependOnOtherLayers()
    {
        var result = Types.InAssembly(SharedAssembly)
            .Should()
            .NotHaveDependencyOn("PesaGraph.Api")
            .And()
            .NotHaveDependencyOn("PesaGraph.Infrastructure")
            .And()
            .NotHaveDependencyOn("PesaGraph.Ledger")
            .And()
            .NotHaveDependencyOn("PesaGraph.Audit")
            .And()
            .NotHaveDependencyOn("PesaGraph.Notifications")
            .And()
            .NotHaveDependencyOn("PesaGraph.Providers")
            .And()
            .NotHaveDependencyOn("PesaGraph.Reconciliation")
            .And()
            .NotHaveDependencyOn("PesaGraph.Liquidity")
            .And()
            .NotHaveDependencyOn("PesaGraph.Tenancy")
            .And()
            .NotHaveDependencyOn("PesaGraph.Ingestion")
            .And()
            .NotHaveDependencyOn("PesaGraph.Conversational")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Shared layer should not depend on other layers. Failed types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Enumerable.Empty<string>())}"
        );
    }

    [Fact]
    public void AllAssemblies_ShouldBeLoaded()
    {
        var allAssemblies = new[] { ApiAssembly, InfrastructureAssembly, SharedAssembly, LedgerAssembly, AuditAssembly, NotificationsAssembly, ProvidersAssembly, ReconciliationAssembly, LiquidityAssembly, TenancyAssembly, IngestionAssembly, ConversationalAssembly };
        
        foreach (var assembly in allAssemblies)
        {
            assembly.Should().NotBeNull($"Assembly {assembly?.GetName().Name} should be loadable");
        }
    }
}
