using System;
using System.Threading.Tasks;
using FluentAssertions;
using PesaGraph.Audit.Repositories;
using PesaGraph.Audit.Services;
using Xunit;

namespace PesaGraph.UnitTests.Audit;

public class AuditServiceTests
{
    private readonly AuditService _sut = new(new InMemoryAuditRepository());
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task RecordAuditAsync_WithValidDetails_PersistsTenantAuditLog()
    {
        var result = await _sut.RecordAuditAsync(TenantId, "operator@example.com", "TransactionResolved", "Transaction", "QKH123", "Resolved manually");

        var logs = await _sut.GetAuditLogsAsync(TenantId);

        result.IsSuccess.Should().BeTrue();
        logs.Value.Should().ContainSingle(log => log.Actor == "operator@example.com" && log.Action == "TransactionResolved");
    }

    [Fact]
    public async Task RecordAuditAsync_WithMissingRequiredValue_ReturnsValidationFailure()
    {
        var result = await _sut.RecordAuditAsync(TenantId, "", "TransactionResolved", "Transaction", "QKH123");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Audit.RequiredFields");
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithInvalidLimit_ReturnsValidationFailure()
    {
        var result = await _sut.GetAuditLogsAsync(TenantId, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Audit.InvalidLimit");
    }
}
