using System.Text.RegularExpressions;
using Extraction.Worker.Models;

namespace Extraction.Worker.Services;

public sealed class ModalityBodyRegionExtractor
{
    private static readonly (Regex Regex, string Modality)[] ModalityPatterns =
    {
        (new Regex(@"\b(?:XRAY|X-RAY|X RAY|RADIOGRAPH)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "XR"),
        (new Regex(@"\b(?:CT|C\.T\.|COMPUTED TOMOGRAPHY)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "CT"),
        (new Regex(@"\b(?:MRI|M\.R\.I\.|MAGNETIC RESONANCE)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "MRI"),
        (new Regex(@"\b(?:US|U/S|ULTRASOUND)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "US"),
        (new Regex(@"\b(?:NM|NUCLEAR MEDICINE|PET|SPECT)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "NM"),
        (new Regex(@"\b(?:IR|INTERVENTIONAL RADIOLOGY|ANGIO)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "IR")
    };

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

    public ModalityBodyRegionResult Extract(string reportText)
    {
        var modality = "UNKNOWN";
        var bodyRegion = "UNKNOWN";
        var modalitySpans = new List<string>();
        var regionSpans = new List<string>();

        foreach (var (regex, value) in ModalityPatterns)
        {
            var match = regex.Match(reportText);
            if (match.Success)
            {
                modality = value;
                modalitySpans.Add(BuildSpan(match.Index, match.Index + match.Length));
                break;
            }
        }

        foreach (var (regex, value) in BodyRegionPatterns)
        {
            var match = regex.Match(reportText);
            if (match.Success)
            {
                bodyRegion = value;
                regionSpans.Add(BuildSpan(match.Index, match.Index + match.Length));
                break;
            }
        }

        return new ModalityBodyRegionResult
        {
            Modality = modality,
            BodyRegion = bodyRegion,
            ModalityEvidenceSpans = modalitySpans,
            BodyRegionEvidenceSpans = regionSpans
        };
    }

    private static string BuildSpan(int start, int end) => $"Report:{start}-{end}";
}
