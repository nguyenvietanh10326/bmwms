using System;
using System.Text.RegularExpressions;

namespace BMWMS.Repository.Common;

public static class StocktakeLocationHelper
{
    private static readonly Regex TargetLocationRegex = new(
        @"\[TARGET_LOCATION:(?<id>\d+)(?::(?<code>[^\]]*))?\]",
        RegexOptions.Compiled);

    public static (long? TargetLocationId, string? TargetLocationCode, string CleanNotes) ParseTargetLocation(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return (null, null, string.Empty);

        var match = TargetLocationRegex.Match(notes);
        if (match.Success && long.TryParse(match.Groups["id"].Value, out var id))
        {
            var code = match.Groups["code"].Success && !string.IsNullOrWhiteSpace(match.Groups["code"].Value)
                ? match.Groups["code"].Value.Trim()
                : null;
            var clean = TargetLocationRegex.Replace(notes, "").Trim();
            return (id, code, clean);
        }

        return (null, null, notes.Trim());
    }

    public static long? ExtractTargetLocationId(string? notes)
    {
        var (targetId, _, _) = ParseTargetLocation(notes);
        return targetId;
    }

    public static string FormatNotesWithTargetLocation(long? targetLocationId, string? targetLocationCode, string? userNotes)
    {
        var (_, _, clean) = ParseTargetLocation(userNotes);
        if (!targetLocationId.HasValue || targetLocationId.Value <= 0)
            return clean;

        var tag = string.IsNullOrWhiteSpace(targetLocationCode)
            ? $"[TARGET_LOCATION:{targetLocationId.Value}]"
            : $"[TARGET_LOCATION:{targetLocationId.Value}:{targetLocationCode.Trim()}]";

        return string.IsNullOrWhiteSpace(clean) ? tag : $"{tag} {clean}";
    }
}
