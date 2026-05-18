using System.Text.RegularExpressions;
using HubPay.Domain.Exceptions;

namespace HubPay.Infrastructure.Payments;

public static partial class PspPhoneValidator
{
    private static readonly Regex E164 = E164Regex();

    public static string RequirePhone(string? phone, string scheme)
    {
        var normalized = Normalize(phone);
        if (normalized is null)
            throw new PspIntegrationException(scheme, $"Telefone E.164 obrigatório para o esquema {scheme}.");
        return normalized;
    }

    public static string? Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var trimmed = phone.Trim().Replace(" ", string.Empty);
        if (!trimmed.StartsWith('+'))
            trimmed = $"+{trimmed}";

        return E164.IsMatch(trimmed) ? trimmed : null;
    }

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164Regex();
}
