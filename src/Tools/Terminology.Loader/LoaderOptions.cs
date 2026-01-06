using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Terminology.Loader;

public sealed class LoaderOptions
{
    public string CodeSystem { get; init; } = "ICD10CM";
    public string CodeVersionId { get; init; } = "ICD10CM_2026";
    public DateOnly EffectiveFrom { get; init; } = new(2025, 10, 1);
    public string InputZip { get; init; } = string.Empty;
    public string ModelId { get; init; } = "fake-embed-1536";
    public bool Embed { get; init; }
    public bool Aliases { get; init; } = true;

    public Guid ResolveCodeVersionGuid()
    {
        if (Guid.TryParse(CodeVersionId, out var parsed))
        {
            return parsed;
        }

        return CreateDeterministicGuid(CodeVersionId);
    }

    public static bool TryParse(string[] args, out LoaderOptions options, out string? error)
    {
        options = new LoaderOptions();
        error = null;

        if (args.Length == 0)
        {
            error = "Missing required arguments. Use --inputZip <path> at minimum.";
            return false;
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = arg.Split('=', 2, StringSplitOptions.TrimEntries);
            var key = parts[0][2..];
            var value = parts.Length == 2 ? parts[1] : (i + 1 < args.Length ? args[++i] : string.Empty);
            map[key] = value;
        }

        if (!map.TryGetValue("inputZip", out var inputZip) || string.IsNullOrWhiteSpace(inputZip))
        {
            error = "Missing --inputZip <path-to-icd10cm-table-and-index-YYYY.zip>.";
            return false;
        }

        var codeSystem = map.TryGetValue("codeSystem", out var cs) && !string.IsNullOrWhiteSpace(cs)
            ? cs
            : options.CodeSystem;
        var codeVersionId = map.TryGetValue("codeVersionId", out var cvid) && !string.IsNullOrWhiteSpace(cvid)
            ? cvid
            : options.CodeVersionId;
        var modelId = map.TryGetValue("modelId", out var mid) && !string.IsNullOrWhiteSpace(mid)
            ? mid
            : options.ModelId;

        var effectiveFrom = options.EffectiveFrom;
        if (map.TryGetValue("effectiveFrom", out var ef) && !string.IsNullOrWhiteSpace(ef))
        {
            if (!DateOnly.TryParse(ef, CultureInfo.InvariantCulture, DateTimeStyles.None, out effectiveFrom))
            {
                error = "Invalid --effectiveFrom value. Use YYYY-MM-DD.";
                return false;
            }
        }

        var embed = map.TryGetValue("embed", out var embedRaw) && bool.TryParse(embedRaw, out var embedFlag)
            ? embedFlag
            : options.Embed;
        var aliases = map.TryGetValue("aliases", out var aliasRaw) && bool.TryParse(aliasRaw, out var aliasFlag)
            ? aliasFlag
            : options.Aliases;

        options = new LoaderOptions
        {
            CodeSystem = codeSystem,
            CodeVersionId = codeVersionId,
            EffectiveFrom = effectiveFrom,
            InputZip = inputZip,
            ModelId = modelId,
            Embed = embed,
            Aliases = aliases
        };

        return true;
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
