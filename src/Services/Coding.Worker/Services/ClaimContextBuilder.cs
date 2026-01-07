using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class ClaimContextBuilder
{
    public ClaimContext Build(
        ExtractedRadiologyEncounter encounter,
        CptCodingResult cptResult,
        RadiologyIcdCodingResult icdResult)
    {
        var claim = new ClaimContext
        {
            Header = new ClaimHeader
            {
                PayerId = string.IsNullOrWhiteSpace(encounter.PayerId) ? "DEFAULT" : encounter.PayerId,
                PlaceOfService = encounter.Sections.TryGetValue("PlaceOfService", out var pos) ? pos : string.Empty,
                DateOfService = encounter.DateOfService
            }
        };

        claim.Procedures.AddRange(BuildProcedures(cptResult, encounter));

        claim.Diagnoses.AddRange(BuildDiagnoses(icdResult));

        claim.Evidence.AddRange(BuildEvidence(encounter));

        return claim;
    }

    private static IEnumerable<ProcedureEntry> BuildProcedures(CptCodingResult cptResult, ExtractedRadiologyEncounter encounter)
    {
        foreach (var primary in cptResult.PrimaryCpts)
        {
            yield return new ProcedureEntry
            {
                Code = primary.Code,
                Units = 1,
                Modifiers = primary.Modifiers.ToList(),
                Laterality = encounter.Laterality ?? string.Empty
            };
        }

        foreach (var addOn in cptResult.AddOnCpts)
        {
            yield return new ProcedureEntry
            {
                Code = addOn.Code,
                Units = 1,
                Modifiers = addOn.Modifiers.ToList(),
                Laterality = encounter.Laterality ?? string.Empty
            };
        }
    }

    private static IEnumerable<DiagnosisEntry> BuildDiagnoses(RadiologyIcdCodingResult icdResult)
    {
        if (icdResult.FinalSelection.PrimaryIcd is not null)
        {
            yield return new DiagnosisEntry
            {
                Code = icdResult.FinalSelection.PrimaryIcd.Code,
                IsPrincipal = true
            };
        }

        foreach (var secondary in icdResult.SecondaryCandidates)
        {
            yield return new DiagnosisEntry
            {
                Code = secondary.Code,
                IsPrincipal = false
            };
        }
    }

    private static IEnumerable<SupportingEvidence> BuildEvidence(ExtractedRadiologyEncounter encounter)
    {
        var index = 0;
        foreach (var span in encounter.IndicationEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "Indication",
                Snippet = ExtractSnippet(encounter, span)
            };
        }

        foreach (var span in encounter.ModalityEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "Modality",
                Snippet = ExtractSnippet(encounter, span)
            };
        }

        foreach (var span in encounter.BodyRegionEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "BodyRegion",
                Snippet = ExtractSnippet(encounter, span)
            };
        }

        foreach (var span in encounter.ContrastEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "Contrast",
                Snippet = ExtractSnippet(encounter, span)
            };
        }

        foreach (var span in encounter.ViewsOrCompletenessEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "ViewsOrCompleteness",
                Snippet = ExtractSnippet(encounter, span)
            };
        }

        foreach (var span in encounter.LateralityEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "Laterality",
                Snippet = ExtractSnippet(encounter, span)
            };
        }

        foreach (var span in encounter.GuidanceEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "Guidance",
                Snippet = ExtractSnippet(encounter, span)
            };
        }

        foreach (var span in encounter.InterventionEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "Intervention",
                Snippet = ExtractSnippet(encounter, span)
            };
        }
    }

    private static string ExtractSnippet(ExtractedRadiologyEncounter encounter, string span)
    {
        if (string.IsNullOrWhiteSpace(span))
        {
            return string.Empty;
        }

        if (!TryParseSpan(span, out var section, out var start, out var end))
        {
            return span;
        }

        if (!string.IsNullOrWhiteSpace(encounter.ReportText) && start >= 0 && end <= encounter.ReportText.Length)
        {
            return SafeSnippet(encounter.ReportText, start, end);
        }

        if (encounter.Sections.TryGetValue(section, out var content))
        {
            if (start >= 0 && end <= content.Length)
            {
                return SafeSnippet(content, start, end);
            }

            return LimitSnippet(content);
        }

        if (!string.IsNullOrWhiteSpace(encounter.ReportText))
        {
            return LimitSnippet(encounter.ReportText);
        }

        return span;
    }

    private static bool TryParseSpan(string span, out string section, out int start, out int end)
    {
        section = string.Empty;
        start = -1;
        end = -1;

        var parts = span.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        section = parts[0];
        var rangeParts = parts[1].Split('-', 2);
        if (rangeParts.Length != 2)
        {
            return false;
        }

        return int.TryParse(rangeParts[0], out start) && int.TryParse(rangeParts[1], out end);
    }

    private static string SafeSnippet(string content, int start, int end)
    {
        if (string.IsNullOrWhiteSpace(content) || start < 0 || end <= start)
        {
            return string.Empty;
        }

        if (start >= content.Length)
        {
            return string.Empty;
        }

        if (end > content.Length)
        {
            end = content.Length;
        }

        return content.Substring(start, end - start).Trim();
    }

    private static string LimitSnippet(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var trimmed = content.Trim();
        return trimmed.Length <= 200 ? trimmed : trimmed.Substring(0, 200);
    }
}
