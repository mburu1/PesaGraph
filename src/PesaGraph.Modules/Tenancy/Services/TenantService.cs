using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;
using PesaGraph.Tenancy.Domain;
using PesaGraph.Tenancy.DTOs;
using PesaGraph.Tenancy.Repositories;

namespace PesaGraph.Tenancy.Services;

public interface ITenantService
{
    Task<Result<TenantDto>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> GetTenantByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> GetTenantByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TenantDto>>> ListTenantsAsync(TenantStatus? status = null, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> UpdateTenantAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result<ApiKeyGeneratedDto>> GenerateApiKeyAsync(Guid tenantId, GenerateApiKeyRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeApiKeyAsync(Guid tenantId, Guid apiKeyId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> AuthenticateApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default);
    Task<Result> SetProviderCredentialAsync(Guid tenantId, SetTenantCredentialRequest request, CancellationToken cancellationToken = default);
}

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;

    public TenantService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantDto>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _tenantRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing != null)
        {
            return Result.Failure<TenantDto>(Error.Conflict("Tenant.CodeExists", $"Tenant with code '{request.Code}' already exists."));
        }

        var tenant = Tenant.Create(request.Name, request.Code, request.ContactEmail, request.ContactPhone);
        await _tenantRepository.AddAsync(tenant, cancellationToken);

        return Result.Success(MapToDto(tenant));
    }

    public async Task<Result<TenantDto>> GetTenantByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure<TenantDto>(Error.NotFound("Tenant.NotFound", $"Tenant with ID '{id}' was not found."));
        }

        return Result.Success(MapToDto(tenant));
    }

    public async Task<Result<TenantDto>> GetTenantByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByCodeAsync(code, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure<TenantDto>(Error.NotFound("Tenant.NotFound", $"Tenant with code '{code}' was not found."));
        }

        return Result.Success(MapToDto(tenant));
    }

    public async Task<Result<IReadOnlyList<TenantDto>>> ListTenantsAsync(TenantStatus? status = null, CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.ListAsync(status, cancellationToken);
        var dtos = tenants.Select(MapToDto).ToList();
        return Result.Success<IReadOnlyList<TenantDto>>(dtos);
    }

    public async Task<Result<TenantDto>> UpdateTenantAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure<TenantDto>(Error.NotFound("Tenant.NotFound", $"Tenant with ID '{id}' was not found."));
        }

        tenant.SetName(request.Name);
        tenant.UpdateContactInfo(request.ContactEmail, request.ContactPhone);
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return Result.Success(MapToDto(tenant));
    }

    public async Task<Result<ApiKeyGeneratedDto>> GenerateApiKeyAsync(Guid tenantId, GenerateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure<ApiKeyGeneratedDto>(Error.NotFound("Tenant.NotFound", $"Tenant with ID '{tenantId}' was not found."));
        }

        // Generate raw API key: "pg_live_" + 32 random bytes in base64
        var rawKeyBytes = RandomNumberGenerator.GetBytes(32);
        var rawKeySuffix = Convert.ToBase64String(rawKeyBytes).Replace("+", "").Replace("/", "").Replace("=", "");
        var rawApiKey = $"pg_{rawKeySuffix}";
        var keyPrefix = rawApiKey[..10];
        var keyHash = ComputeSha256Hash(rawApiKey);

        var apiKey = tenant.GenerateApiKey(request.KeyName, keyHash, keyPrefix, request.ExpiresAtUtc);
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return Result.Success(new ApiKeyGeneratedDto(
            apiKey.Id,
            apiKey.Name,
            apiKey.KeyPrefix,
            rawApiKey,
            apiKey.ExpiresAtUtc));
    }

    public async Task<Result> RevokeApiKeyAsync(Guid tenantId, Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", $"Tenant with ID '{tenantId}' was not found."));
        }

        tenant.RevokeApiKey(apiKeyId);
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<Guid>> AuthenticateApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return Result.Failure<Guid>(Error.Unauthorized("ApiKey.Empty", "API key was not provided."));
        }

        var keyHash = ComputeSha256Hash(rawApiKey);
        var tenant = await _tenantRepository.GetByApiKeyHashAsync(keyHash, cancellationToken);

        if (tenant == null || tenant.Status != TenantStatus.Active)
        {
            return Result.Failure<Guid>(Error.Unauthorized("ApiKey.Invalid", "Invalid or inactive API key."));
        }

        var apiKey = tenant.ApiKeys.FirstOrDefault(k => k.KeyHash == keyHash);
        apiKey?.RecordUsage();
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return Result.Success(tenant.Id);
    }

    public async Task<Result> SetProviderCredentialAsync(Guid tenantId, SetTenantCredentialRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", $"Tenant with ID '{tenantId}' was not found."));
        }

        tenant.SetCredential(request.Provider, request.JsonPayload);
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return Result.Success();
    }

    private static TenantDto MapToDto(Tenant tenant) =>
        new(tenant.Id, tenant.Name, tenant.Code, tenant.ContactEmail, tenant.ContactPhone, tenant.Status, tenant.CreatedAtUtc);

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexStringLower(bytes);
    }
}
