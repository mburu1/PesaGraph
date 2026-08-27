using System;
using FluentAssertions;
using PesaGraph.Tenancy.Domain;
using Xunit;

namespace PesaGraph.UnitTests.Tenancy;

public class TenantTests
{
    private static Tenant CreateTenant(
        string name = "Acme Payments",
        string code = "ACME",
        string email = "admin@acme.co.ke",
        string phone = "+254712345678")
    {
        return Tenant.Create(name, code, email, phone);
    }

    [Fact]
    public void Create_ShouldReturnTenantWithExpectedProperties()
    {
        var tenant = CreateTenant();

        tenant.Should().NotBeNull();
        tenant.Id.Should().NotBe(Guid.Empty);
        tenant.Name.Should().Be("Acme Payments");
        tenant.Code.Should().Be("ACME");
        tenant.ContactEmail.Should().Be("admin@acme.co.ke");
        tenant.ContactPhone.Should().Be("+254712345678");
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Create_ShouldNormalizeCodeToUppercase()
    {
        var tenant = Tenant.Create("Test", "mixed", "test@test.com", "+254700000000");

        tenant.Code.Should().Be("MIXED");
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowercase()
    {
        var tenant = Tenant.Create("Test", "TEST", "ADMIN@TEST.COM", "+254700000000");

        tenant.ContactEmail.Should().Be("admin@test.com");
    }

    [Fact]
    public void SetName_WithValidName_ShouldUpdateName()
    {
        var tenant = CreateTenant();

        tenant.SetName("New Name Ltd");

        tenant.Name.Should().Be("New Name Ltd");
        tenant.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void SetName_WithEmptyName_ShouldThrowArgumentException()
    {
        var tenant = CreateTenant();

        var act = () => tenant.SetName("   ");

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void SetCode_WithValidCode_ShouldUpdateCode()
    {
        var tenant = CreateTenant();

        tenant.SetCode("newcode");

        tenant.Code.Should().Be("NEWCODE");
    }

    [Fact]
    public void SetCode_WithEmptyCode_ShouldThrowArgumentException()
    {
        var tenant = CreateTenant();

        var act = () => tenant.SetCode("");

        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Fact]
    public void UpdateContactInfo_ShouldUpdateEmailAndPhone()
    {
        var tenant = CreateTenant();

        tenant.UpdateContactInfo("NEW@ACME.CO.KE", "+254799000000");

        tenant.ContactEmail.Should().Be("new@acme.co.ke");
        tenant.ContactPhone.Should().Be("+254799000000");
        tenant.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void UpdateStatus_ShouldChangeTenantStatus()
    {
        var tenant = CreateTenant();

        tenant.UpdateStatus(TenantStatus.Suspended);

        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public void GenerateApiKey_ShouldAddKeyToCollection()
    {
        var tenant = CreateTenant();

        var key = tenant.GenerateApiKey("MyApp Key", "hashed-value", "pg_live_");

        tenant.ApiKeys.Should().ContainSingle();
        key.Name.Should().Be("MyApp Key");
        key.IsActive.Should().BeTrue();
    }

    [Fact]
    public void GenerateApiKey_WithExpiry_ShouldSetExpiry()
    {
        var tenant = CreateTenant();
        var expiry = DateTimeOffset.UtcNow.AddDays(30);

        var key = tenant.GenerateApiKey("Expiring Key", "hash", "pg_", expiry);

        key.ExpiresAtUtc.Should().Be(expiry);
    }

    [Fact]
    public void RevokeApiKey_ShouldMarkKeyAsRevoked()
    {
        var tenant = CreateTenant();
        var key = tenant.GenerateApiKey("Key To Revoke", "hash", "pg_");

        tenant.RevokeApiKey(key.Id);

        tenant.ApiKeys.Should().ContainSingle(k => !k.IsActive);
    }

    [Fact]
    public void SetCredential_NewProvider_ShouldAddCredential()
    {
        var tenant = CreateTenant();

        tenant.SetCredential("MPesa", "encrypted-payload");

        tenant.Credentials.Should().ContainSingle(c => c.Provider == "mpesa");
    }

    [Fact]
    public void SetCredential_ExistingProvider_ShouldUpdateCredential()
    {
        var tenant = CreateTenant();
        tenant.SetCredential("MPesa", "old-payload");

        tenant.SetCredential("MPesa", "new-payload");

        tenant.Credentials.Should().ContainSingle();
        tenant.Credentials.Should().ContainSingle(c => c.EncryptedJsonPayload == "new-payload");
    }

    [Fact]
    public void SetCredential_CaseInsensitive_ShouldMatchExistingProvider()
    {
        var tenant = CreateTenant();
        tenant.SetCredential("mpesa", "v1");

        tenant.SetCredential("MPESA", "v2");

        tenant.Credentials.Should().ContainSingle();
    }
}
