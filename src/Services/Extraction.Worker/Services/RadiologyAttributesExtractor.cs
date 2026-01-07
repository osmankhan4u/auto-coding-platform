using System.Text.RegularExpressions;
using Extraction.Worker.Models;

namespace Extraction.Worker.Services;

public sealed class RadiologyAttributesExtractor
{
    private static readonly (Regex Regex, string Region)[] BodyRegionPatterns =
    {
        (new Regex(@"\b(?:ABDOMEN\s*(?:/|AND)?\s*PELVIS|ABD(?:OMEN)?\s*(?:/|AND)?\s*PEL(?:VIS)?|ABDOMINOPELVIC)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled), "ABD_PELVIS"),
        (new Regex(@"\bABDOMEN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "ABDOMEN"),
        (new Regex(@"\bPELVIS\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "PELVIS"),
        (new Regex(@"\b(?:CHEST|THORAX)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "CHEST"),
        (new Regex(@"\b(?:BRAIN|HEAD)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "BRAIN_HEAD"),
        (new Regex(@"\bSHOULDER\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "SHOULDER"),
        (new Regex(@"\bKNEE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "KNEE")
    };

    private static readonly (Regex Regex, string Laterality)[] LateralityPatterns =
    {
        (new Regex(@"\bBILATERAL(?:LY)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "BILATERAL"),
        (new Regex(@"\bRIGHT\b|\bRT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "RT"),
        (new Regex(@"\bLEFT\b|\bLT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "LT")
    };

    private static readonly (Regex Regex, string Contrast)[] ContrastPatterns =
    {
        (new Regex(@"\b(?:WITH\s+AND\s+WITHOUT\s+CONTRAST|W/\s*WO|W\s*/\s*WO)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "WITH_AND_WITHOUT"),
        (new Regex(@"\b(?:WITHOUT\s+CONTRAST|NON\s*-?\s*CONTRAST|NONCONTRAST)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "WITHOUT"),
        (new Regex(@"\b(?:WITH\s+CONTRAST|POST\s+CONTRAST|CONTRAST\s+ENHANCED)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "WITH")
    };

    private static readonly (Regex Regex, string Views)[] ViewPatterns =
    {
        (new Regex(@"\b(?:FOUR|4)\s+VIEWS?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "VIEWS_4_PLUS"),
        (new Regex(@"\b(?:THREE|3)\s+VIEWS?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "VIEWS_3"),
        (new Regex(@"\b(?:TWO|2)\s+VIEWS?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "VIEWS_2")
    };

    private static readonly (Regex Regex, string Completeness)[] UltrasoundCompletenessPatterns =
    {
        (new Regex(@"\bCOMPLETE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "US_COMPLETE"),
        (new Regex(@"\bLIMITED\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "US_LIMITED")
    };

    private static readonly Regex GuidanceRegex = new(@"\b(?:GUIDANCE|GUIDED|FLUOROSCOPIC GUIDANCE|CT GUIDANCE|US GUIDANCE|ULTRASOUND GUIDANCE)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InterventionRegex = new(@"\b(?:BIOPSY|DRAINAGE|INJECTION|ASPIRATION|ABLATION|EMBOLIZATION|STENT|THROMBECTOMY|ANGIOPLASTY|CATHETER|PLACEMENT)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public RadiologyAttributesResult Extract(string reportText, IReadOnlyDictionary<string, SectionInfo> sections)
    {
        var text = reportText;
        var baseOffset = 0;
        if (sections.TryGetValue("Technique", out var technique) &&
            !string.IsNullOrWhiteSpace(technique.ContentText))
        {
            text = technique.ContentText;
            baseOffset = technique.ContentStart < 0 ? 0 : technique.ContentStart;
        }

        var regions = new List<string>();
        var regionSpans = new List<string>();
        foreach (var (regex, region) in BodyRegionPatterns)
        {
            foreach (Match match in regex.Matches(text))
            {
                if (!match.Success)
                {
                    continue;
                }

                if (!regions.Contains(region, StringComparer.OrdinalIgnoreCase))
                {
                    regions.Add(region);
                }

                regionSpans.Add(BuildSpan("Report", baseOffset + match.Index, baseOffset + match.Index + match.Length));
            }
        }

        var laterality = "NONE";
        var lateralitySpans = new List<string>();
        var lateralityHits = new List<string>();
        foreach (var (regex, value) in LateralityPatterns)
        {
            foreach (Match match in regex.Matches(text))
            {
                if (!match.Success)
                {
                    continue;
                }

                if (!lateralityHits.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    lateralityHits.Add(value);
                }

                lateralitySpans.Add(BuildSpan("Report", baseOffset + match.Index, baseOffset + match.Index + match.Length));
            }
        }

        if (lateralityHits.Contains("BILATERAL", StringComparer.OrdinalIgnoreCase) ||
            (lateralityHits.Contains("RT", StringComparer.OrdinalIgnoreCase) &&
             lateralityHits.Contains("LT", StringComparer.OrdinalIgnoreCase)))
        {
            laterality = "BILATERAL";
        }
        else if (lateralityHits.Contains("RT", StringComparer.OrdinalIgnoreCase))
        {
            laterality = "RT";
        }
        else if (lateralityHits.Contains("LT", StringComparer.OrdinalIgnoreCase))
        {
            laterality = "LT";
        }

        var contrast = "UNKNOWN";
        var contrastSpans = new List<string>();
        foreach (var (regex, value) in ContrastPatterns)
        {
            var match = regex.Match(text);
            if (match.Success)
            {
                contrast = value;
                contrastSpans.Add(BuildSpan("Report", baseOffset + match.Index, baseOffset + match.Index + match.Length));
                break;
            }
        }

        var viewsOrCompleteness = "UNKNOWN";
        var viewsSpans = new List<string>();
        foreach (var (regex, value) in ViewPatterns)
        {
            var match = regex.Match(text);
            if (match.Success)
            {
                viewsOrCompleteness = value;
                viewsSpans.Add(BuildSpan("Report", baseOffset + match.Index, baseOffset + match.Index + match.Length));
                break;
            }
        }

        if (viewsOrCompleteness == "UNKNOWN")
        {
            foreach (var (regex, value) in UltrasoundCompletenessPatterns)
            {
                var match = regex.Match(text);
                if (match.Success)
                {
                    viewsOrCompleteness = value;
                    viewsSpans.Add(BuildSpan("Report", baseOffset + match.Index, baseOffset + match.Index + match.Length));
                    break;
                }
            }
        }

        var guidanceSpans = new List<string>();
        var guidanceMatch = GuidanceRegex.Match(text);
        var guidanceFlag = guidanceMatch.Success;
        if (guidanceMatch.Success)
        {
            guidanceSpans.Add(BuildSpan("Report", baseOffset + guidanceMatch.Index, baseOffset + guidanceMatch.Index + guidanceMatch.Length));
        }

        var interventionSpans = new List<string>();
        var interventionMatch = InterventionRegex.Match(text);
        var interventionFlag = interventionMatch.Success;
        if (interventionMatch.Success)
        {
            interventionSpans.Add(BuildSpan("Report", baseOffset + interventionMatch.Index, baseOffset + interventionMatch.Index + interventionMatch.Length));
        }

        return new RadiologyAttributesResult
        {
            BodyRegions = regions,
            Laterality = laterality,
            ContrastState = contrast,
            ViewsOrCompleteness = viewsOrCompleteness,
            GuidanceFlag = guidanceFlag,
            InterventionFlag = interventionFlag,
            BodyRegionEvidenceSpans = regionSpans,
            LateralityEvidenceSpans = lateralitySpans,
            ContrastEvidenceSpans = contrastSpans,
            ViewsOrCompletenessEvidenceSpans = viewsSpans,
            GuidanceEvidenceSpans = guidanceSpans,
            InterventionEvidenceSpans = interventionSpans
        };
    }

    private static string BuildSpan(string source, int start, int end) => $"{source}:{start}-{end}";
}
