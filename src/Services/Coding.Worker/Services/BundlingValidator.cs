using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class BundlingValidator : IBundlingValidator
{
    public BundlingValidationResult Validate(ExtractedRadiologyEncounter encounter, CptCodingResult cptResult)
    {
        var result = new BundlingValidationResult
        {
            WasValidated = true,
            IsPlaceholder = false,
            ValidatorVersion = "1.0",
            Issues = new List<string>(),
            Notes = new List<string>()
        };

        if (cptResult.PrimaryCpts.Count == 0 && cptResult.AddOnCpts.Count == 0)
        {
            result.Notes.Add("No CPT codes available for bundling validation.");
            return result;
        }

        AddDuplicateIssues(cptResult, result);
        ApplyGuidanceBundlingRules(cptResult, result);

        if (result.Issues.Count == 0)
        {
            result.Notes.Add("No bundling conflicts detected.");
        }

        return result;
    }

    private static void AddDuplicateIssues(CptCodingResult cptResult, BundlingValidationResult result)
    {
        var allSelections = cptResult.PrimaryCpts.Concat(cptResult.AddOnCpts).ToList();
        var duplicates = allSelections
            .GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var code in duplicates)
        {
            result.Issues.Add($"DUPLICATE_CPT:{code}");
        }
    }

    private static void ApplyGuidanceBundlingRules(CptCodingResult cptResult, BundlingValidationResult result)
    {
        var guidanceAddOnCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "77012",
            "76942",
            "77002"
        };

        var primaryCodesIncludingGuidance = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "49406"
        };

        var bundledPrimary = cptResult.PrimaryCpts
            .Where(item => primaryCodesIncludingGuidance.Contains(item.Code))
            .Select(item => item.Code)
            .ToList();

        if (bundledPrimary.Count == 0)
        {
            return;
        }

        foreach (var addOn in cptResult.AddOnCpts)
        {
            if (!guidanceAddOnCodes.Contains(addOn.Code))
            {
                continue;
            }

            addOn.ExclusionReasons.Add("BUNDLED_WITH_PRIMARY");
            addOn.Rationale = string.IsNullOrWhiteSpace(addOn.Rationale)
                ? "Guidance add-on bundled with primary procedure."
                : $"{addOn.Rationale} Guidance add-on bundled with primary procedure.";

            foreach (var primary in bundledPrimary)
            {
                result.Issues.Add($"GUIDANCE_BUNDLED_WITH:{primary}:{addOn.Code}");
            }
        }

        if (result.Issues.Count > 0)
        {
            cptResult.RequiresHumanReview = true;
        }
    }
}
