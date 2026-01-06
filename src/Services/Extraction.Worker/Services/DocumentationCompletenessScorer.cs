using System.Text.RegularExpressions;
using Extraction.Worker.Models;

namespace Extraction.Worker.Services;

public sealed class DocumentationCompletenessScorer
{
    private static readonly Regex SignatureRegex = new(
        @"^(?:signed|electronically signed|e-signed|dictated|radiologist)\b",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public DocumentationCompletenessResult Evaluate(
        string reportText,
        IReadOnlyDictionary<string, SectionInfo> sections)
    {
        var warnings = new List<string>();
        var score = 0.0;

        score += AddSectionScore(sections, "Indication", 0.40, warnings, "MISSING_INDICATION_SECTION");
        score += AddSectionScore(sections, "Technique", 0.15, warnings, "MISSING_TECHNIQUE_SECTION");
        score += AddSectionScore(sections, "Impression", 0.35, warnings, "MISSING_IMPRESSION_SECTION");

        if (SignatureRegex.IsMatch(reportText))
        {
            score += 0.10;
        }
        else
        {
            warnings.Add("MISSING_SIGNATURE_SECTION");
        }

        return new DocumentationCompletenessResult
        {
            Score = Math.Round(score, 2),
            Warnings = warnings
        };
    }

    private static double AddSectionScore(
        IReadOnlyDictionary<string, SectionInfo> sections,
        string sectionName,
        double weight,
        List<string> warnings,
        string warning)
    {
        if (sections.TryGetValue(sectionName, out var section) &&
            !string.IsNullOrWhiteSpace(section.ContentText))
        {
            return weight;
        }

        warnings.Add(warning);
        return 0.0;
    }
}
