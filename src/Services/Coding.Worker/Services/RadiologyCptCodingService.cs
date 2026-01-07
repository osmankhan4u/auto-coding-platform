using System.Text.RegularExpressions;
using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class RadiologyCptCodingService
{
    private static readonly Regex CtguidanceRegex = new(@"\bCT GUIDANCE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UsguidanceRegex = new(@"\b(?:US GUIDANCE|ULTRASOUND GUIDANCE)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FluoroGuidanceRegex = new(@"\bFLUORO(?:SCOPIC)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DrainageRegex = new(@"\bDRAINAGE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BiopsyRegex = new(@"\bBIOPSY\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CptCodingResult Generate(ExtractedRadiologyEncounter encounter)
    {
        var result = new CptCodingResult();
        var evidence = BuildEvidence(encounter);
        var modifiers = BuildModifiers(encounter);

        if (string.Equals(encounter.Modality, "CT", StringComparison.OrdinalIgnoreCase))
        {
            if (HasRegion(encounter, "CHEST"))
            {
                var code = MapCtChest(encounter);
                if (code is not null)
                {
                    result.PrimaryCpts.Add(BuildSelection(code.Value.code, code.Value.description, "CPT_CT_CHEST", evidence, modifiers));
                }
                else
                {
                    result.ExclusionReasons.Add("CT chest missing or unknown contrast state.");
                }
            }

            if (HasRegion(encounter, "ABD_PELVIS") || (HasRegion(encounter, "ABDOMEN") && HasRegion(encounter, "PELVIS")))
            {
                var code = MapCtAbdPelvis(encounter);
                if (code is not null)
                {
                    result.PrimaryCpts.Add(BuildSelection(code.Value.code, code.Value.description, "CPT_CT_ABD_PELVIS", evidence, modifiers));
                }
                else
                {
                    result.ExclusionReasons.Add("CT abdomen/pelvis missing or unknown contrast state.");
                }
            }
        }

        if (string.Equals(encounter.Modality, "MRI", StringComparison.OrdinalIgnoreCase) && HasRegion(encounter, "SHOULDER"))
        {
            var code = MapMriShoulder(encounter);
            if (code is not null)
            {
                result.PrimaryCpts.Add(BuildSelection(code.Value.code, code.Value.description, "CPT_MRI_SHOULDER", evidence, modifiers));
            }
            else
            {
                result.ExclusionReasons.Add("MRI shoulder missing or unknown contrast state.");
            }
        }

        if (string.Equals(encounter.Modality, "US", StringComparison.OrdinalIgnoreCase) && HasRegion(encounter, "ABDOMEN"))
        {
            var code = MapUsAbdomen(encounter);
            if (code is not null)
            {
                result.PrimaryCpts.Add(BuildSelection(code.Value.code, code.Value.description, "CPT_US_ABDOMEN", evidence, modifiers));
            }
            else
            {
                result.ExclusionReasons.Add("US abdomen missing completeness (complete vs limited).");
            }
        }

        if (string.Equals(encounter.Modality, "XR", StringComparison.OrdinalIgnoreCase) && HasRegion(encounter, "KNEE"))
        {
            var code = MapXrKnee(encounter);
            if (code is not null)
            {
                result.PrimaryCpts.Add(BuildSelection(code.Value.code, code.Value.description, "CPT_XR_KNEE", evidence, modifiers));
            }
            else
            {
                result.ExclusionReasons.Add("XR knee missing view count.");
            }
        }

        if ((string.Equals(encounter.Modality, "IR", StringComparison.OrdinalIgnoreCase) || encounter.InterventionFlag) &&
            TryMapIrProcedure(encounter, out var irCode))
        {
            result.PrimaryCpts.Add(BuildSelection(irCode.code, irCode.description, "CPT_IR_PROC", evidence, modifiers));
        }

        if (encounter.GuidanceFlag)
        {
            var guidanceCode = MapGuidanceAddOn(encounter);
            if (guidanceCode is not null)
            {
                result.AddOnCpts.Add(BuildSelection(guidanceCode.Value.code, guidanceCode.Value.description, "CPT_GUIDANCE_ADDON", evidence, new List<string>()));
            }
        }

        result.RequiresHumanReview = result.PrimaryCpts.Count == 0;
        return result;
    }

    private static (string code, string description)? MapCtChest(ExtractedRadiologyEncounter encounter) =>
        encounter.ContrastState?.ToUpperInvariant() switch
        {
            "WITHOUT" => ("71250", "CT chest without contrast"),
            "WITH" => ("71260", "CT chest with contrast"),
            "WITH_AND_WITHOUT" => ("71270", "CT chest with and without contrast"),
            _ => null
        };

    private static (string code, string description)? MapCtAbdPelvis(ExtractedRadiologyEncounter encounter) =>
        encounter.ContrastState?.ToUpperInvariant() switch
        {
            "WITHOUT" => ("74176", "CT abdomen and pelvis without contrast"),
            "WITH" => ("74177", "CT abdomen and pelvis with contrast"),
            "WITH_AND_WITHOUT" => ("74178", "CT abdomen and pelvis with and without contrast"),
            _ => null
        };

    private static (string code, string description)? MapMriShoulder(ExtractedRadiologyEncounter encounter) =>
        encounter.ContrastState?.ToUpperInvariant() switch
        {
            "WITHOUT" => ("73221", "MRI shoulder without contrast"),
            "WITH" => ("73222", "MRI shoulder with contrast"),
            "WITH_AND_WITHOUT" => ("73223", "MRI shoulder with and without contrast"),
            _ => null
        };

    private static (string code, string description)? MapUsAbdomen(ExtractedRadiologyEncounter encounter) =>
        encounter.ViewsOrCompleteness?.ToUpperInvariant() switch
        {
            "US_COMPLETE" => ("76700", "Ultrasound abdomen complete"),
            "US_LIMITED" => ("76705", "Ultrasound abdomen limited"),
            _ => null
        };

    private static (string code, string description)? MapXrKnee(ExtractedRadiologyEncounter encounter) =>
        encounter.ViewsOrCompleteness?.ToUpperInvariant() switch
        {
            "VIEWS_2" => ("73560", "X-ray knee 1-2 views"),
            "VIEWS_3" => ("73562", "X-ray knee 3 views"),
            "VIEWS_4_PLUS" => ("73564", "X-ray knee 4+ views"),
            _ => null
        };

    private static bool TryMapIrProcedure(ExtractedRadiologyEncounter encounter, out (string code, string description) selection)
    {
        var reportText = BuildReportText(encounter);
        if (DrainageRegex.IsMatch(reportText))
        {
            selection = ("49406", "Percutaneous drainage with imaging guidance");
            return true;
        }

        if (BiopsyRegex.IsMatch(reportText))
        {
            selection = ("49180", "Percutaneous biopsy, deep tissue");
            return true;
        }

        selection = default;
        return false;
    }

    private static (string code, string description)? MapGuidanceAddOn(ExtractedRadiologyEncounter encounter)
    {
        var reportText = BuildReportText(encounter);
        if (CtguidanceRegex.IsMatch(reportText))
        {
            return ("77012", "CT guidance for needle placement");
        }

        if (UsguidanceRegex.IsMatch(reportText) || string.Equals(encounter.Modality, "US", StringComparison.OrdinalIgnoreCase))
        {
            return ("76942", "Ultrasound guidance for needle placement");
        }

        if (FluoroGuidanceRegex.IsMatch(reportText))
        {
            return ("77002", "Fluoroscopic guidance for needle placement");
        }

        return null;
    }

    private static string BuildReportText(ExtractedRadiologyEncounter encounter) =>
        string.Join("\n", encounter.Sections.Values);

    private static bool HasRegion(ExtractedRadiologyEncounter encounter, string region)
    {
        if (encounter.BodyRegions.Any(item => string.Equals(item, region, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return string.Equals(encounter.BodyRegion, region, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> BuildEvidence(ExtractedRadiologyEncounter encounter)
    {
        var spans = new List<string>();
        spans.AddRange(encounter.ModalityEvidenceSpans);
        spans.AddRange(encounter.BodyRegionEvidenceSpans);
        spans.AddRange(encounter.ContrastEvidenceSpans);
        spans.AddRange(encounter.ViewsOrCompletenessEvidenceSpans);
        spans.AddRange(encounter.LateralityEvidenceSpans);
        spans.AddRange(encounter.GuidanceEvidenceSpans);
        spans.AddRange(encounter.InterventionEvidenceSpans);
        return spans.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> BuildModifiers(ExtractedRadiologyEncounter encounter)
    {
        var modifiers = new List<string>();
        switch (encounter.BillingContext?.ToUpperInvariant())
        {
            case "PROFESSIONAL":
                modifiers.Add("26");
                break;
            case "TECHNICAL":
                modifiers.Add("TC");
                break;
        }

        switch (encounter.Laterality?.ToUpperInvariant())
        {
            case "RT":
                modifiers.Add("RT");
                break;
            case "LT":
                modifiers.Add("LT");
                break;
            case "BILATERAL":
                modifiers.Add("50");
                break;
        }

        return modifiers;
    }

    private static CptCodeSelection BuildSelection(
        string code,
        string description,
        string ruleId,
        List<string> evidenceSpans,
        List<string> modifiers)
    {
        return new CptCodeSelection
        {
            Code = code,
            Description = description,
            Modifiers = modifiers,
            RuleId = ruleId,
            RuleVersion = "1.0",
            Confidence = 0.95,
            EvidenceSpans = evidenceSpans,
            ExclusionReasons = new List<string>(),
            Rationale = "Deterministic CPT mapping from structured fields."
        };
    }
}
