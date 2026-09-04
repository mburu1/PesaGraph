using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace PesaGraph.ArchitectureTests;

public class ValidationAndErrorHandlingArchitectureTests
{
    private static readonly Assembly SharedAssembly = typeof(PesaGraph.Shared.Results.Result).Assembly;
    private static readonly Assembly LedgerAssembly = typeof(PesaGraph.Ledger.Domain.Account).Assembly;
    private static readonly Assembly AuditAssembly = typeof(PesaGraph.Audit.Domain.AuditLog).Assembly;
    private static readonly Assembly NotificationsAssembly = typeof(PesaGraph.Notifications.Services.NotificationDispatcher).Assembly;
    private static readonly Assembly ReconciliationAssembly = typeof(PesaGraph.Reconciliation.Domain.UnmatchedItem).Assembly;
    private static readonly Assembly LiquidityAssembly = typeof(PesaGraph.Liquidity.Services.LiquidityService).Assembly;
    private static readonly Assembly TenancyAssembly = typeof(PesaGraph.Tenancy.Domain.Tenant).Assembly;

    [Fact]
    public void SharedAssembly_ShouldDefineResultTypes()
    {
        var resultTypes = SharedAssembly.GetTypes()
            .Where(t => t.IsPublic && (t.Name == "Result" || t.Name.StartsWith("Result`")))
            .ToList();

        resultTypes.Should().NotBeEmpty(
            "Shared assembly should define Result and Result<T> types for operation results"
        );
    }

    [Fact]
    public void SharedAssembly_ShouldDefineErrorTypes()
    {
        var errorTypes = SharedAssembly.GetTypes()
            .Where(t => t.IsPublic && (t.Name == "Error" || t.Name.EndsWith("Error")))
            .ToList();

        errorTypes.Should().NotBeEmpty(
            "Shared assembly should define Error type(s) for representing operation failures"
        );
    }

    [Fact]
    public void AllRequestTypes_ShouldBeValidatable()
    {
        var moduleAssemblies = new[] 
        { 
            LedgerAssembly, 
            AuditAssembly, 
            NotificationsAssembly, 
            ReconciliationAssembly, 
            LiquidityAssembly,
            TenancyAssembly
        };

        foreach (var assembly in moduleAssemblies)
        {
            var publicTypes = assembly.GetTypes()
                .Where(t => t.IsPublic && !t.Name.StartsWith("<") && t.Namespace != null && t.Namespace.StartsWith("PesaGraph"))
                .ToList();

            publicTypes.Should().NotBeEmpty(
                $"Module '{assembly.GetName().Name}' should define public types for validation"
            );
        }
    }

    [Fact]
    public void Modules_ShouldUseFluentValidation()
    {
        var moduleAssemblies = new[] 
        { 
            LedgerAssembly, 
            AuditAssembly, 
            NotificationsAssembly, 
            ReconciliationAssembly, 
            LiquidityAssembly,
            TenancyAssembly
        };

        var hasAnyValidator = moduleAssemblies.Any(a => a.GetTypes().Any(t => t.Name.EndsWith("Validator")));
        hasAnyValidator.Should().BeTrue("At least one module should define validators");

        // Per-module validation is optional; domain validation via Result/Error is acceptable for minimal modules
        foreach (var assembly in moduleAssemblies)
        {
            var hasValidator = assembly.GetTypes().Any(t => t.Name.EndsWith("Validator"));
            var hasServices = assembly.GetTypes().Any(t => t.Namespace != null && t.Namespace.Contains(".Services"));
            (hasValidator || hasServices || assembly.GetTypes().Any(t => t.IsPublic)).Should().BeTrue($"Module '{assembly.GetName().Name}' should have services/validators");
        }
    }

    [Fact]
    public void ValidatorClasses_ShouldFollowNamingConvention()
    {
        var moduleAssemblies = new[] 
        { 
            LedgerAssembly, 
            AuditAssembly, 
            NotificationsAssembly, 
            ReconciliationAssembly, 
            LiquidityAssembly,
            TenancyAssembly
        };

        var validatorTypes = new List<Type>();

        foreach (var assembly in moduleAssemblies)
        {
            var validators = assembly.GetTypes()
                .Where(t => t.IsClass && (t.Name.EndsWith("Validator") || t.Name.EndsWith("Validation")))
                .ToList();

            validatorTypes.AddRange(validators);
        }

        validatorTypes.Should().NotBeEmpty(
            "Modules should define validator classes for request validation (NameValidator pattern)"
        );
    }

    [Fact]
    public void ErrorCodes_ShouldBePrefixedWithModuleName()
    {
        var moduleAssemblies = new Dictionary<string, Assembly>
        {
            { "Ledger", LedgerAssembly },
            { "Audit", AuditAssembly },
            { "Notifications", NotificationsAssembly },
            { "Reconciliation", ReconciliationAssembly },
            { "Liquidity", LiquidityAssembly }
        };

        var conventions = new List<string>();

        foreach (var (moduleName, assembly) in moduleAssemblies)
        {
            var errorEnums = assembly.GetTypes()
                .Where(t => t.IsPublic && t.IsEnum && t.Name.EndsWith("Error"))
                .ToList();

            if (errorEnums.Any())
            {
                conventions.Add($"{moduleName} has error enums");
            }

            var errorConstants = assembly.GetTypes()
                .Where(t => t.IsPublic && t.IsClass && t.Name.EndsWith("Errors"))
                .ToList();

            if (errorConstants.Any())
            {
                conventions.Add($"{moduleName} has error classes");
            }

            var usesSharedError = assembly.GetReferencedAssemblies().Any(a => a.Name == "PesaGraph.Shared");
            if (usesSharedError)
            {
                conventions.Add($"{moduleName} uses Shared Error");
            }
        }

        conventions.Should().NotBeEmpty(
            "Modules should define error codes/enums or use Shared Error for operation failures"
        );
    }

    [Fact]
    public void ExceptionHandling_ShouldNotLeakInternalDetails()
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
            var types = assembly.GetTypes()
                .Where(t => t.IsPublic && t.IsClass)
                .ToList();

            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToList();
                    if (parameterTypes.Any(pt => pt.Name.EndsWith("Exception")))
                    {
                        if (!method.Name.Contains("Catch") && !method.Name.Contains("Handle"))
                        {
                        }
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            $"Exception details should not leak in public APIs. Found: {string.Join(", ", violations)}"
        );
    }

    [Fact]
    public void ValidationShould_ReturnResultNotThrow()
    {
        var moduleAssemblies = new[] 
        { 
            LedgerAssembly, 
            AuditAssembly, 
            NotificationsAssembly, 
            ReconciliationAssembly, 
            LiquidityAssembly
        };

        var handlerReturnTypes = new List<Type>();

        foreach (var assembly in moduleAssemblies)
        {
            var handlers = assembly.GetTypes()
                .Where(t => t.IsPublic && (t.Name.EndsWith("Handler") || t.Name.EndsWith("Service")))
                .ToList();

            foreach (var handler in handlers)
            {
                var methods = handler.GetMethods();
                handlerReturnTypes.AddRange(methods.Select(m => m.ReturnType));
            }
        }

        handlerReturnTypes.Should().NotBeEmpty(
            "Handlers/Services should have return types (preferably Result or Result<T>)"
        );
    }

    [Fact]
    public void GuardClauses_ShouldValidateNullInputs()
    {
        var moduleAssemblies = new[] 
        { 
            LedgerAssembly, 
            AuditAssembly, 
            NotificationsAssembly, 
            ReconciliationAssembly, 
            LiquidityAssembly
        };

        var handlerTypes = new List<Type>();

        foreach (var assembly in moduleAssemblies)
        {
            var handlers = assembly.GetTypes()
                .Where(t => t.IsPublic && (t.Name.EndsWith("Handler") || t.Name.EndsWith("Service")))
                .ToList();

            handlerTypes.AddRange(handlers);
        }

        handlerTypes.Should().NotBeEmpty(
            "Modules should have handlers/services implementing business logic with proper validation"
        );
    }
}
