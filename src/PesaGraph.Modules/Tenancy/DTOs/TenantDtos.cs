using System;
using System.Collections.Generic;
using PesaGraph.Tenancy.Domain;

namespace PesaGraph.Tenancy.DTOs;

public record TenantDto(
    Guid Id,
    string Name,
    string Code,
    string ContactEmail,
    string ContactPhone,
    TenantStatus Status,
    DateTimeOffset CreatedAtUtc);

public record CreateTenantRequest(
    string Name,
    string Code,
    string ContactEmail,
    string ContactPhone);

public record UpdateTenantRequest(
    string Name,
    string ContactEmail,
    string ContactPhone);

public record GenerateApiKeyRequest(
    string KeyName,
    DateTimeOffset? ExpiresAtUtc = null);

public record ApiKeyGeneratedDto(
    Guid KeyId,
    string KeyName,
    string KeyPrefix,
    string RawApiKey,
    DateTimeOffset? ExpiresAtUtc);

public record SetTenantCredentialRequest(
    string Provider,
    string JsonPayload);
