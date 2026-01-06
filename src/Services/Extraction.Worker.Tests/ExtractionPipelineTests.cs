using Extraction.Worker.Models;
using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class ExtractionPipelineTests
{
    [Fact]
    public void Extract_EndToEndReportsProduceExpectedAssertions()
    {
        var service = CreateService();
        var cases = new[]
        {
            new
            {
                Report =
                    "INDICATION: Chest pain\nTECHNIQUE: CT CHEST W CONTRAST\nFINDINGS: No pneumothorax. Pneumonia present.\nIMPRESSION: Pulmonary embolism not seen.",
                Expectations = new[]
                {
                    ("pneumothorax", "RULED_OUT"),
                    ("pneumonia", "CONFIRMED"),
                    ("pulmonary embolism", "RULED_OUT")
                }
            },
            new
            {
                Report =
                    "INDICATION: RLQ pain\nTECHNIQUE: CT ABDOMEN\nFINDINGS: Possible appendicitis.",
                Expectations = new[]
                {
                    ("appendicitis", "SUSPECTED")
                }
            },
            new
            {
                Report =
                    "INDICATION: Headache\nTECHNIQUE: MRI BRAIN\nIMPRESSION: Chronic infarct.",
                Expectations = new[]
                {
                    ("infarct", "CONFIRMED")
                }
            },
            new
            {
                Report =
                    "INDICATION: RUQ pain\nTECHNIQUE: US ABDOMEN\nFINDINGS: Gallstones present.",
                Expectations = new[]
                {
                    ("cholelithiasis", "CONFIRMED")
                }
            },
            new
            {
                Report =
                    "INDICATION: Abdominal pain\nTECHNIQUE: CT ABDOMEN AND PELVIS\nFINDINGS: No bowel obstruction.",
                Expectations = new[]
                {
                    ("bowel obstruction", "RULED_OUT")
                }
            },
            new
            {
                Report =
                    "INDICATION: SOB\nTECHNIQUE: CT CHEST\nFINDINGS: No PE or pneumothorax; pneumonia present.",
                Expectations = new[]
                {
                    ("pulmonary embolism", "RULED_OUT"),
                    ("pneumothorax", "RULED_OUT"),
                    ("pneumonia", "CONFIRMED")
                }
            }
        };

        foreach (var testCase in cases)
        {
            var result = service.Extract("enc-1", testCase.Report);
            Assert.NotEmpty(result.Concepts);
            Assert.All(result.Concepts, concept => Assert.NotEmpty(concept.EvidenceSpans));

            foreach (var (text, certainty) in testCase.Expectations)
            {
                var concept = result.Concepts.FirstOrDefault(item =>
                    string.Equals(item.Text, text, StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(concept);
                Assert.Equal(certainty, concept!.Certainty);
            }
        }
    }

    [Fact]
    public void Extract_AddsPackWarningsWhenOnlyGlobalApplies()
    {
        var service = CreateService();
        var report = "INDICATION: Pain\nFINDINGS: Mass noted.";

        var result = service.Extract("enc-2", report);

        Assert.Contains("CONCEPT_PACK_FALLBACK_GLOBAL_ONLY", result.Warnings);
        Assert.Contains("MODALITY_UNKNOWN", result.Warnings);
        Assert.Contains("BODY_REGION_UNKNOWN", result.Warnings);
    }

    [Fact]
    public void Extract_AddsNoMatchWarningWhenRegistryHasNoPatterns()
    {
        var service = CreateService(new ConceptPackRegistry(Array.Empty<(string Name, List<ConceptPattern> Patterns)>()));
        var report = "INDICATION: Pain\nFINDINGS: Mass noted.";

        var result = service.Extract("enc-3", report);

        Assert.Contains("CONCEPT_PACK_NO_MATCH", result.Warnings);
    }

    private static RadiologyExtractionService CreateService(ConceptPackRegistry? registry = null)
    {
        var sectionDetector = new SectionDetector();
        var sentenceSplitter = new SentenceSplitter();
        var negationResolver = new NegationScopeResolver();
        var uncertaintyResolver = new UncertaintyScopeResolver();
        var historyResolver = new HistoryScopeResolver();
        var targetAwareResolver = new TargetAwareNegationResolver();
        var extractor = new ClinicalConceptExtractor(sentenceSplitter, negationResolver, uncertaintyResolver, historyResolver, targetAwareResolver);
        var completeness = new DocumentationCompletenessScorer();
        var modalityExtractor = new ModalityBodyRegionExtractor();
        registry ??= new ConceptPackRegistry();

        return new RadiologyExtractionService(sectionDetector, modalityExtractor, registry, extractor, completeness);
    }
}
