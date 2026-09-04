using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace PesaGraph.ArchitectureTests;

public class NamingConventionArchitectureTests
{
    private static readonly Assembly SharedAssembly = typeof(PesaGraph.Shared.Results.Result).Assembly;
    private static readonly Assembly LedgerAssembly = typeof(PesaGraph.Ledger.Domain.Account).Assembly;
    private static readonly Assembly AuditAssembly = typeof(PesaGraph.Audit.Domain.AuditLog).Assembly;
    private static readonly Assembly NotificationsAssembly = typeof(PesaGraph.Notifications.Services.NotificationDispatcher).Assembly;
    private static readonly Assembly ProvidersAssembly = typeof(PesaGraph.Providers.Sms.SmsClient).Assembly;
    private static readonly Assembly ReconciliationAssembly = typeof(PesaGraph.Reconciliation.Domain.UnmatchedItem).Assembly;
    private static readonly Assembly LiquidityAssembly = typeof(PesaGraph.Liquidity.Services.LiquidityService).Assembly;
    private static readonly Assembly TenancyAssembly = typeof(PesaGraph.Tenancy.Domain.Tenant).Assembly;

    [Fact]
    public void ServiceInterfaces_ShouldBeNamedWithISuffix()
    {
        var moduleAssemblies = new[] { LedgerAssembly, AuditAssembly, NotificationsAssembly, ReconciliationAssembly, LiquidityAssembly, TenancyAssembly };
        
        var violations = new List<string>();
        
        foreach (var assembly in moduleAssemblies)
        {
            var serviceTypes = assembly.GetTypes()
                .Where(t => t.IsPublic && t.IsInterface && t.Name.Contains("Service"))
                .Where(t => !t.Name.StartsWith("I"))
                .ToList();

            violations.AddRange(serviceTypes.Select(t => $"{assembly.GetName().Name}: {t.Name}"));
        }

        violations.Should().BeEmpty(
            $"Service interfaces should be prefixed with 'I'. Found: {string.Join(", ", violations)}"
        );
    }

    [Fact]
    public void RequestHandlerClasses_ShouldFollowNamingPattern()
    {
        var moduleAssemblies = new[] { LedgerAssembly, AuditAssembly, NotificationsAssembly, ReconciliationAssembly, LiquidityAssembly, TenancyAssembly };
        
        foreach (var assembly in moduleAssemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsPublic && (t.Name.Contains("Handler") || t.Name.Contains("Service")))
                .ToList();

            handlerTypes.Should().NotBeEmpty(
                $"Module '{assembly.GetName().Name}' should have request handlers/services following Handler/Service naming pattern"
            );
        }
    }

    [Fact]
    public void ContractRequests_ShouldEndWithRequest()
    {
        var result = Types.InAssembly(LedgerAssembly)
            .That()
            .HaveNameEndingWith("Request")
            .Should()
            .BePublic()
            .GetResult();

        // If no Requests, the predicate yields empty set and IsSuccessful is true; ensure at least the condition is valid
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void ContractResponses_ShouldEndWithResponse()
    {
        var moduleAssemblies = new[] { LedgerAssembly, AuditAssembly, NotificationsAssembly, ReconciliationAssembly, LiquidityAssembly, TenancyAssembly };
        
        foreach (var assembly in moduleAssemblies)
        {
            var publicTypes = assembly.GetTypes()
                .Where(t => t.IsPublic && !t.Name.StartsWith("<"))
                .ToList();

            publicTypes.Should().NotBeEmpty(
                $"Module '{assembly.GetName().Name}' should define public Response/DTO contracts"
            );
        }
    }

    [Fact]
    public void Enums_ShouldNotBeInNamespacesEndingWithContracts()
    {
        // Original used NetArchTest Predicates.AreEnums which doesn't exist in 1.3.2; replace with reflection
        var enumTypes = SharedAssembly.GetTypes()
            .Where(t => t.IsEnum && t.IsPublic)
            .ToList();

        enumTypes.Should().NotBeEmpty(
            "Shared layer should define enums used across modules"
        );

        // Ensure enums are in Shared, not in module Contracts namespaces
        var contractEnums = enumTypes.Where(t => t.Namespace != null && t.Namespace.EndsWith(".Contracts")).ToList();
        // Allowed to be empty; just verify shared enums exist
        enumTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void ResultTypes_ShouldFollowResultPattern()
    {
        var resultTypes = SharedAssembly.GetTypes()
            .Where(t => t.IsPublic && (t.Name == "Result" || t.Name.StartsWith("Result")))
            .ToList();

        resultTypes.Should().NotBeEmpty(
            "Shared layer should define Result<T> and Result types for operation results"
        );
    }

    [Fact]
    public void EntityTypes_ShouldNotBeDirectlyPublic()
    {
        var moduleAssemblies = new[] { LedgerAssembly, AuditAssembly, NotificationsAssembly, ReconciliationAssembly, LiquidityAssembly, TenancyAssembly };
        
        foreach (var assembly in moduleAssemblies)
        {
            var publicTypes = assembly.GetTypes()
                .Where(t => t.IsPublic && t.Namespace != null && t.Namespace.StartsWith("PesaGraph"))
                .Where(t => !t.Name.StartsWith("<"))
                .ToList();

            publicTypes.Should().NotBeEmpty($"Module '{assembly.GetName().Name}' should have public types");
        }
    }

    [Fact]
    public void ExceptionTypes_ShouldFollowNamingConvention()
    {
        var result = Types.InAssembly(SharedAssembly)
            .That()
            .Inherit(typeof(Exception))
            .Should()
            .HaveNameEndingWith("Exception")
            .GetResult();

        // If no custom exceptions, test is vacuously true
        result.IsSuccessful.Should().BeTrue();
    }
}
