using System.Text.Json;

namespace BMWMS.Web.Services;

public static class ApiErrorReader
{
    public static async Task<string> ReadAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var document = JsonDocument.Parse(raw);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .SelectMany(field => field.Value.ValueKind == JsonValueKind.Array
                                ? field.Value.EnumerateArray().Select(value => value.GetString() ?? string.Empty)
                                : Enumerable.Empty<string>())
                            .Where(message => !string.IsNullOrWhiteSpace(message))
                            .Distinct().Take(5).ToArray();
                        if (messages.Length > 0) return string.Join("; ", messages);
                    }
                    foreach (var key in new[] { "message", "detail", "title" })
                        if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(value.GetString())) return value.GetString()!;
                }
            }
            catch (JsonException) { /* Old endpoints may return plain text. */ }
            if (raw.Length < 300 && !raw.TrimStart().StartsWith('<')) return raw.Trim().Trim('"');
        }
        return $"Máy chủ trả lỗi HTTP {(int)response.StatusCode}. Vui lòng tải lại và thử lại.";
    }
}
