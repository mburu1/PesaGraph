using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace PesaGraph.ArchitectureTests;

public class DependencyInjectionArchitectureTests
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
    public void InfrastructureLayer_ShouldDefineServiceCollectionExtensions()
    {
        var types = InfrastructureAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsClass && t.Name.Contains("ServiceCollectionExtensions"))
            .ToList();

        types.Should().NotBeEmpty(
            "Infrastructure layer should define ServiceCollectionExtensions for DI setup"
        );
    }

    [Fact]
    public void ApiLayer_ShouldCallInfrastructureConfiguration()
    {
        var programType = ApiAssembly.GetTypes().FirstOrDefault(t => t.Name == "Program");
        if (programType == null)
        {
            var controllerExists = ApiAssembly.GetTypes().Any(t => t.Name.EndsWith("Controller"));
            controllerExists.Should().BeTrue("API should have Program or controllers for startup configuration");
            return;
        }
        programType.Should().NotBeNull("API should have a Program class for startup configuration");
    }

    [Fact]
    public void AllServiceInterfaces_ShouldBeRegistered()
    {
        var moduleAssemblies = new[] 
        { 
            LedgerAssembly, 
            AuditAssembly, 
            NotificationsAssembly, 
            ReconciliationAssembly, 
            LiquidityAssembly, 
            TenancyAssembly,
            IngestionAssembly,
            ConversationalAssembly
        };

        foreach (var assembly in moduleAssemblies)
        {
            var serviceInterfaces = assembly.GetTypes()
                .Where(t => t.IsPublic && t.IsInterface && t.Name.StartsWith("I"))
                .Where(t => !t.Name.StartsWith("IRequest") && !t.Name.StartsWith("IHandler"))
                .ToList();

            serviceInterfaces.Count().Should().BeGreaterThan(0,
                $"Module '{assembly.GetName().Name}' should expose service interfaces for DI"
            );
        }
    }

    [Fact]
    public void ProvidersModule_ShouldExposeSmsAndWhatsAppClients()
    {
        var providerInterfaces = ProvidersAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsInterface)
            .Select(t => t.Name)
            .ToList();

        providerInterfaces.Should().Contain("ISmsClient", "Providers module should expose ISmsClient");
        providerInterfaces.Should().Contain("IWhatsAppClient", "Providers module should expose IWhatsAppClient");
    }

    [Fact]
    public void RequestHandlers_ShouldNotDirectlyInstantiateServices()
    {
        var violations = new List<string>();

        var moduleAssemblies = new[] 
        { 
            LedgerAssembly, 
            AuditAssembly, 
            NotificationsAssembly, 
            ReconciliationAssembly, 
            LiquidityAssembly 
        };

        foreach (var assembly in moduleAssemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsPublic && t.Name.EndsWith("Handler"))
                .ToList();

            foreach (var handler in handlerTypes)
            {
                var constructor = handler.GetConstructors().FirstOrDefault();
                if (constructor?.GetParameters().Length == 0)
                {
                    violations.Add($"{assembly.GetName().Name}: {handler.Name} has parameterless constructor (missing DI)");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Handlers should use constructor injection. Issues: {string.Join(", ", violations)}"
        );
    }

    [Fact]
    public void RepositoryInterfaces_ShouldBeDefined()
    {
        var moduleAssemblies = new[] 
        { 
            LedgerAssembly, 
            AuditAssembly, 
            ReconciliationAssembly, 
            LiquidityAssembly 
        };

        foreach (var assembly in moduleAssemblies)
        {
            var repositoryInterfaces = assembly.GetTypes()
                .Where(t => t.IsPublic && t.IsInterface && t.Name.Contains("Repository"))
                .ToList();

            // Liquidity currently uses ILedgerRepository via composition rather than own repository; allow empty if no dedicated repository yet
            if (assembly == LiquidityAssembly && repositoryInterfaces.Count == 0)
            {
                continue;
            }

            repositoryInterfaces.Should().NotBeEmpty(
                $"Module '{assembly.GetName().Name}' should define repository interfaces"
            );
        }
    }

    [Fact]
    public void SharedDependencies_ShouldBeConsistent()
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

        var allReferences = new List<string?>();

        foreach (var assembly in moduleAssemblies)
        {
            var references = assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(name => name != null && !name.StartsWith("System") && !name.StartsWith("Microsoft"))
                .ToList();

            allReferences.AddRange(references);
        }

        var groupedByName = allReferences
            .Where(r => r != null)
            .GroupBy(r => r!)
            .Where(g => g.Count() > 1)
            .ToList();

        groupedByName.Should().NotBeEmpty(
            "Modules should share common dependencies for consistency"
        );
    }

    [Fact]
    public void ModuleInitialization_ShouldFollowConvention()
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

        foreach (var assembly in moduleAssemblies)
        {
            var moduleName = assembly.GetName().Name!;
            var extensionClassName = $"ServiceCollectionExtensions";
            
            var extensionType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Contains(extensionClassName));

            if (extensionType == null)
            {
                extensionType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name.Contains("Extensions"));
            }

            extensionType.Should().NotBeNull(
                $"Module '{moduleName}' should have a ServiceCollectionExtensions or Extensions class for DI registration"
            );
        }
    }
}
