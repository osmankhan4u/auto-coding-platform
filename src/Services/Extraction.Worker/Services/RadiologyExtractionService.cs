using Extraction.Worker.Models;

namespace Extraction.Worker.Services;

public sealed class RadiologyExtractionService
{
    private readonly SectionDetector _sectionDetector;
    private readonly ModalityBodyRegionExtractor _modalityBodyRegionExtractor;
    private readonly RadiologyAttributesExtractor _attributesExtractor;
    private readonly ConceptPackRegistry _conceptPackRegistry;
    private readonly ClinicalConceptExtractor _conceptExtractor;
    private readonly DocumentationCompletenessScorer _completenessScorer;

    public RadiologyExtractionService(
        SectionDetector sectionDetector,
        ModalityBodyRegionExtractor modalityBodyRegionExtractor,
        RadiologyAttributesExtractor attributesExtractor,
        ConceptPackRegistry conceptPackRegistry,
        ClinicalConceptExtractor conceptExtractor,
        DocumentationCompletenessScorer completenessScorer)
    {
        _sectionDetector = sectionDetector;
        _modalityBodyRegionExtractor = modalityBodyRegionExtractor;
        _attributesExtractor = attributesExtractor;
        _conceptPackRegistry = conceptPackRegistry;
        _conceptExtractor = conceptExtractor;
        _completenessScorer = completenessScorer;
    }

    public ExtractedRadiologyEncounter Extract(string encounterId, string reportText)
    {
        var sectionResult = _sectionDetector.Detect(reportText);
        var modalityResult = _modalityBodyRegionExtractor.Extract(reportText);
        var attributesResult = _attributesExtractor.Extract(reportText, sectionResult.Sections);
        var packResolution = _conceptPackRegistry.Resolve(modalityResult.Modality, modalityResult.BodyRegion);
        var concepts = _conceptExtractor.Extract(reportText, sectionResult.Sections, packResolution.Patterns);
        var completeness = _completenessScorer.Evaluate(reportText, sectionResult.Sections);

        var warnings = new List<string>();
        warnings.AddRange(completeness.Warnings);

        if (string.Equals(modalityResult.Modality, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("MODALITY_UNKNOWN");
        }

        if (string.Equals(modalityResult.BodyRegion, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("BODY_REGION_UNKNOWN");
        }

        if (packResolution.AppliedPacks.Count == 1 &&
            string.Equals(packResolution.AppliedPacks[0], "GLOBAL", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("CONCEPT_PACK_FALLBACK_GLOBAL_ONLY");
        }

        if (packResolution.Patterns.Count == 0)
        {
            warnings.Add("CONCEPT_PACK_NO_MATCH");
        }

        var sections = sectionResult.Sections.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ContentText.Trim());

        sections.TryGetValue("Indication", out var indicationText);
        var indicationSpans = new List<string>();
        if (sectionResult.Sections.TryGetValue("Indication", out var indicationSection) &&
            indicationSection.ContentStart >= 0 &&
            indicationSection.ContentEnd >= indicationSection.ContentStart)
        {
            indicationSpans.Add($"Indication:{indicationSection.ContentStart}-{indicationSection.ContentEnd}");
        }

        var impressionConcepts = concepts
            .Where(concept => string.Equals(concept.SourcePriority, "IMPRESSION", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var bodyRegions = attributesResult.BodyRegions.Count == 0 &&
                          !string.Equals(modalityResult.BodyRegion, "UNKNOWN", StringComparison.OrdinalIgnoreCase)
            ? new List<string> { modalityResult.BodyRegion }
            : attributesResult.BodyRegions;

        return new ExtractedRadiologyEncounter
        {
            EncounterId = encounterId,
            ReportText = reportText,
            Modality = modalityResult.Modality,
            BodyRegion = modalityResult.BodyRegion,
            BodyRegions = bodyRegions,
            Laterality = attributesResult.Laterality,
            ContrastState = attributesResult.ContrastState,
            ViewsOrCompleteness = attributesResult.ViewsOrCompleteness,
            GuidanceFlag = attributesResult.GuidanceFlag,
            InterventionFlag = attributesResult.InterventionFlag,
            IndicationText = indicationText,
            IndicationEvidenceSpans = indicationSpans,
            Sections = sections,
            DocumentationCompleteness = new DocumentationCompleteness
            {
                Score = completeness.Score
            },
            Warnings = warnings,
            Concepts = concepts,
            ImpressionConcepts = impressionConcepts,
            ModalityEvidenceSpans = modalityResult.ModalityEvidenceSpans,
            BodyRegionEvidenceSpans = attributesResult.BodyRegionEvidenceSpans.Count > 0
                ? attributesResult.BodyRegionEvidenceSpans
                : modalityResult.BodyRegionEvidenceSpans,
            LateralityEvidenceSpans = attributesResult.LateralityEvidenceSpans,
            ContrastEvidenceSpans = attributesResult.ContrastEvidenceSpans,
            ViewsOrCompletenessEvidenceSpans = attributesResult.ViewsOrCompletenessEvidenceSpans,
            GuidanceEvidenceSpans = attributesResult.GuidanceEvidenceSpans,
            InterventionEvidenceSpans = attributesResult.InterventionEvidenceSpans
        };
    }
}
