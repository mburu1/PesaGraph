using System;
using FluentAssertions;
using PesaGraph.Shared.Domain.ValueObjects;
using Xunit;

namespace PesaGraph.UnitTests.Shared;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldRoundToTwoDecimalPlaces()
    {
        var money = new Money(100.555m, "KES");

        money.Amount.Should().Be(100.56m);
    }

    [Fact]
    public void Constructor_ShouldUppercaseCurrency()
    {
        var money = new Money(50m, "kes");

        money.Currency.Should().Be("KES");
    }

    [Fact]
    public void Constructor_WithEmptyCurrency_ShouldThrowArgumentException()
    {
        var act = () => new Money(100m, "");

        act.Should().Throw<ArgumentException>().WithParameterName("currency");
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        var zero = Money.Zero();

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("KES");
    }

    [Fact]
    public void Addition_WithSameCurrency_ShouldAddAmounts()
    {
        var a = new Money(100m, "KES");
        var b = new Money(50m, "KES");

        var result = a + b;

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("KES");
    }

    [Fact]
    public void Addition_WithDifferentCurrency_ShouldThrow()
    {
        var a = new Money(100m, "KES");
        var b = new Money(50m, "USD");

        var act = () => { var _ = a + b; };

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtraction_WithSameCurrency_ShouldSubtractAmounts()
    {
        var a = new Money(100m, "KES");
        var b = new Money(30m, "KES");

        var result = a - b;

        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void GreaterThan_ShouldCompareCorrectly()
    {
        var a = new Money(200m, "KES");
        var b = new Money(100m, "KES");

        (a > b).Should().BeTrue();
        (b > a).Should().BeFalse();
    }

    [Fact]
    public void LessThan_ShouldCompareCorrectly()
    {
        var a = new Money(50m, "KES");
        var b = new Money(150m, "KES");

        (a < b).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var money = new Money(1500.75m, "KES");

        money.ToString().Should().Be("KES 1,500.75");
    }

    [Fact]
    public void Equality_WithSameAmountAndCurrency_ShouldBeEqual()
    {
        var a = new Money(100m, "KES");
        var b = new Money(100m, "KES");

        a.Should().Be(b);
    }

    [Fact]
    public void FromKes_ShouldCreateKesMoneyRecord()
    {
        var money = Money.FromKes(999m);

        money.Currency.Should().Be("KES");
        money.Amount.Should().Be(999m);
    }
}
