using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class SafetyGate
{
    public SafetyGateResult Evaluate(ExtractedRadiologyEncounter encounter)
    {
        var flags = new List<string>();

        if (!HasIndicationOrImpression(encounter))
        {
            flags.Add("MISSING_INDICATION_OR_IMPRESSION");
        }

        if (encounter.DocumentationCompleteness.Score < 0.70)
        {
            flags.Add("LOW_DOCUMENTATION_COMPLETENESS");
        }

        if (encounter.Warnings.Any(warning =>
                string.Equals(warning, "CONCEPT_PACK_FALLBACK_GLOBAL_ONLY", StringComparison.OrdinalIgnoreCase)))
        {
            flags.Add("PACK_FALLBACK_GLOBAL_ONLY");
        }

        if (string.IsNullOrWhiteSpace(encounter.Modality) ||
            string.Equals(encounter.Modality, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
        {
            flags.Add("MODALITY_UNKNOWN");
        }

        return new SafetyGateResult(flags.Count == 0, flags);
    }

    private static bool HasIndicationOrImpression(ExtractedRadiologyEncounter encounter)
    {
        if (!string.IsNullOrWhiteSpace(encounter.IndicationText))
        {
            return true;
        }

        if (encounter.ImpressionConcepts is { Count: > 0 })
        {
            return true;
        }

        if (encounter.Sections is { Count: > 0 } &&
            encounter.Sections.TryGetValue("Indication", out var indicationText))
        {
            return !string.IsNullOrWhiteSpace(indicationText);
        }

        if (encounter.Sections is { Count: > 0 } &&
            encounter.Sections.TryGetValue("Impression", out var impressionText))
        {
            return !string.IsNullOrWhiteSpace(impressionText);
        }

        return false;
    }
}

public sealed record SafetyGateResult(bool CanAutoSelect, List<string> Flags);
