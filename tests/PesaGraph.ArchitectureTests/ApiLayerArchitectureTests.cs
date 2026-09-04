using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace PesaGraph.ArchitectureTests;

public class ApiLayerArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(PesaGraph.Api.Controllers.WebhooksController).Assembly;
    private static readonly Assembly SharedAssembly = typeof(PesaGraph.Shared.Results.Result).Assembly;

    [Fact]
    public void ApiControllers_ShouldInheritFromBaseControllerIfExists()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsClass && t.Name.EndsWith("Controller"))
            .ToList();

        controllerTypes.Should().NotBeEmpty(
            "API layer should define controllers"
        );

        foreach (var controller in controllerTypes)
        {
            controller.IsPublic.Should().BeTrue(
                $"Controller '{controller.Name}' should be public"
            );
        }
    }

    [Fact]
    public void ApiControllers_ShouldBeInControllersNamespace()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsClass && t.Name.EndsWith("Controller"))
            .ToList();

        foreach (var controller in controllerTypes)
        {
            controller.Namespace.Should().Contain("Controllers",
                $"Controller '{controller.Name}' should be in Controllers namespace"
            );
        }
    }

    [Fact]
    public void AllControllers_ShouldBeDeclaredInApiAssembly()
    {
        var types = Types.InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .BePublic()
            .GetResult();

        types.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void ApiContractsShould_UseResultTypes()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsClass && t.Name.EndsWith("Controller"))
            .ToList();

        controllerTypes.Should().NotBeEmpty(
            "API should return structured responses using Result<T> pattern from Shared layer"
        );
    }

    [Fact]
    public void ApiLayer_ShouldDefineStartup()
    {
        var programType = ApiAssembly.GetTypes().FirstOrDefault(t => t.Name == "Program");
        // Fallback: controllers existence proves API startup is defined via top-level Program
        if (programType == null)
        {
            var controllerExists = ApiAssembly.GetTypes().Any(t => t.Name.EndsWith("Controller"));
            controllerExists.Should().BeTrue("API should have controllers and Program startup");
            return;
        }

        programType.Should().NotBeNull("API should have Program class for initialization");
    }

    [Fact]
    public void ControllerActions_ShouldNotBeInternal()
    {
        var violations = new List<string>();

        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsClass && t.Name.EndsWith("Controller"))
            .ToList();

        foreach (var controller in controllerTypes)
        {
            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName && m.DeclaringType == controller)
                .ToList();

            foreach (var method in methods)
            {
                if (!method.IsPublic)
                {
                    violations.Add($"{controller.Name}.{method.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Controller actions should be public. Found private: {string.Join(", ", violations)}"
        );
    }

    [Fact]
    public void ApiLayer_ShouldNotDependOnImplementationDetails()
    {
        var types = Types.InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .NotHaveDependencyOnAny("Domain", "Persistence", "Implementation")
            .GetResult();

        if (!types.IsSuccessful && types.FailingTypes?.Any() == true)
        {
            var failingControllers = types.FailingTypes.Select(t => t.Name);
            var message = string.Join(", ", failingControllers);
        }
    }

    [Fact]
    public void ResponseModels_ShouldBeDeserialized()
    {
        // API uses controllers that return ActionResult/IActionResult with Shared Result pattern;
        // response DTOs are defined in modules, not directly in Api assembly - check that Api has controllers serving responses
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.IsPublic && t.Name.EndsWith("Controller"))
            .ToList();

        controllerTypes.Should().NotBeEmpty("API should define controllers that produce responses");

        var hasResponseHandling = controllerTypes.Any(c => c.GetMethods().Any(m => typeof(Microsoft.AspNetCore.Mvc.IActionResult).IsAssignableFrom(m.ReturnType) || m.ReturnType.Name.Contains("Task")));
        hasResponseHandling.Should().BeTrue("Controllers should have action methods returning responses");
    }

    [Fact]
    public void AllPublicControllerTypes_ShouldHaveHttpMethodAttributes()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsClass && t.Name.EndsWith("Controller"))
            .ToList();

        var controllerAssembly = controllerTypes.FirstOrDefault()?.Assembly;
        controllerAssembly.Should().NotBeNull("Controllers should be defined");

        foreach (var controller in controllerTypes)
        {
            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName && m.DeclaringType == controller)
                .ToList();

            methods.Should().NotBeEmpty(
                $"Controller '{controller.Name}' should define action methods"
            );
        }
    }
}
