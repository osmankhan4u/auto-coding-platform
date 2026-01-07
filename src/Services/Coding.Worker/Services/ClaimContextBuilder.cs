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
                Snippet = span
            };
        }

        foreach (var span in encounter.ModalityEvidenceSpans)
        {
            yield return new SupportingEvidence
            {
                EvidenceId = $"EVID-{index++:D4}",
                Source = "Modality",
                Snippet = span
            };
        }
    }
}
