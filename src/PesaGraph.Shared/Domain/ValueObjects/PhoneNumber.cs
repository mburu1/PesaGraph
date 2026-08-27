using System.Text.RegularExpressions;
using PesaGraph.Shared.Domain;

namespace PesaGraph.Shared.Domain.ValueObjects;

public partial record PhoneNumber : ValueObject
{
    private static readonly Regex KenyanPhoneRegex = new(@"^(?:\+254|254|0)?(7\d{8}|1\d{8})$", RegexOptions.Compiled);

    public string Value { get; }

    public PhoneNumber(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            throw new ArgumentException("Phone number cannot be empty.", nameof(rawPhone));
        }

        var cleaned = rawPhone.Trim().Replace(" ", "").Replace("-", "");
        var match = KenyanPhoneRegex.Match(cleaned);

        if (!match.Success)
        {
            throw new ArgumentException($"'{rawPhone}' is not a valid Kenyan phone number.", nameof(rawPhone));
        }

        // Canonical format: 254XXXXXXXXX
        Value = $"254{match.Groups[1].Value}";
    }

    public static bool TryParse(string? rawPhone, out PhoneNumber? phoneNumber)
    {
        phoneNumber = null;
        if (string.IsNullOrWhiteSpace(rawPhone)) return false;

        var cleaned = rawPhone.Trim().Replace(" ", "").Replace("-", "");
        var match = KenyanPhoneRegex.Match(cleaned);

        if (!match.Success) return false;

        phoneNumber = new PhoneNumber(cleaned);
        return true;
    }

    public override string ToString() => Value;
}
