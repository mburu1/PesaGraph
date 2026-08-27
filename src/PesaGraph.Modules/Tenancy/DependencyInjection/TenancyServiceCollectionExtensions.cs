using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Tenancy.Repositories;
using PesaGraph.Tenancy.Services;
using PesaGraph.Tenancy.Validators;

namespace PesaGraph.Tenancy.DependencyInjection;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddValidatorsFromAssemblyContaining<CreateTenantRequestValidator>();

        return services;
    }
}
