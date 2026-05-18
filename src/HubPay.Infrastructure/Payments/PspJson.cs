using System.Text.Json;

namespace HubPay.Infrastructure.Payments;

internal static class PspJson
{
    public static string? ReadString(JsonElement element, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (element.TryGetProperty(path, out var prop))
            {
                var value = prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.GetRawText();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        return null;
    }

    public static JsonElement? ReadObject(JsonElement element, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (element.TryGetProperty(path, out var prop) && prop.ValueKind == JsonValueKind.Object)
                return prop;
        }
        return null;
    }
}
