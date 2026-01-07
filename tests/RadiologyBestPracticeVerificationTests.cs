using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Coding.Worker.Contracts;
using Coding.Worker.Models;
using Coding.Worker.Services;
using Extraction.Worker.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace RadiologyBestPracticeVerificationTests;

public sealed class RadiologyBestPracticeVerificationTests
{
    [Fact]
    public async Task SampleReportsMeetBestPracticeExpectations()
    {
        var extractionService = CreateExtractionService();
        var (codingService, bundlingValidator) = CreateCodingService();

        var sampleRoot = Path.Combine(FindRepoRoot(), "src", "Services", "Extraction.Worker", "samples", "_in");
        var cases = new[]
        {
            new SampleExpectation(
                "case01_rule_out_pe.txt",
                Modality: "CT",
                BodyRegions: new[] { "CHEST" },
                Laterality: "NONE",
                ContrastState: "WITH",
                ViewsOrCompleteness: "UNKNOWN",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "71260" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: Array.Empty<string>(),
                ForbiddenIcds: new[] { "I26.99" }),
            new SampleExpectation(
                "case02_ct_chest_w_wo.txt",
                Modality: "CT",
                BodyRegions: new[] { "CHEST" },
                Laterality: "NONE",
                ContrastState: "WITH_AND_WITHOUT",
                ViewsOrCompleteness: "UNKNOWN",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "71270" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: new[] { "J18.9" },
                ForbiddenIcds: Array.Empty<string>()),
            new SampleExpectation(
                "case03_us_abdomen_complete.txt",
                Modality: "US",
                BodyRegions: new[] { "ABDOMEN" },
                Laterality: "NONE",
                ContrastState: "UNKNOWN",
                ViewsOrCompleteness: "US_COMPLETE",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "76700" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: new[] { "K80.20" },
                ForbiddenIcds: Array.Empty<string>()),
            new SampleExpectation(
                "case04_us_abdomen_limited.txt",
                Modality: "US",
                BodyRegions: new[] { "ABDOMEN" },
                Laterality: "NONE",
                ContrastState: "UNKNOWN",
                ViewsOrCompleteness: "US_LIMITED",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "76705" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: Array.Empty<string>(),
                ForbiddenIcds: new[] { "N13.30" }),
            new SampleExpectation(
                "case05_xr_knee_2views.txt",
                Modality: "XR",
                BodyRegions: new[] { "KNEE" },
                Laterality: "NONE",
                ContrastState: "UNKNOWN",
                ViewsOrCompleteness: "VIEWS_2",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "73560" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: Array.Empty<string>(),
                ForbiddenIcds: new[] { "S82.90" }),
            new SampleExpectation(
                "case06_xr_knee_4views.txt",
                Modality: "XR",
                BodyRegions: new[] { "KNEE" },
                Laterality: "NONE",
                ContrastState: "UNKNOWN",
                ViewsOrCompleteness: "VIEWS_4_PLUS",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "73564" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: new[] { "S82.90" },
                ForbiddenIcds: Array.Empty<string>()),
            new SampleExpectation(
                "case07_ct_abd_pelvis_with_contrast.txt",
                Modality: "CT",
                BodyRegions: new[] { "ABD_PELVIS" },
                Laterality: "NONE",
                ContrastState: "WITH",
                ViewsOrCompleteness: "UNKNOWN",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "74177" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: new[] { "K35.80" },
                ForbiddenIcds: new[] { "N28.1" }),
            new SampleExpectation(
                "case08_mri_shoulder_right_wo.txt",
                Modality: "MRI",
                BodyRegions: new[] { "SHOULDER" },
                Laterality: "RT",
                ContrastState: "WITHOUT",
                ViewsOrCompleteness: "UNKNOWN",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "73221" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: new[] { "D49.9" },
                ForbiddenIcds: Array.Empty<string>()),
            new SampleExpectation(
                "case09_ir_drainage_guided.txt",
                Modality: "IR",
                BodyRegions: Array.Empty<string>(),
                Laterality: "NONE",
                ContrastState: "UNKNOWN",
                ViewsOrCompleteness: "UNKNOWN",
                GuidanceFlag: true,
                InterventionFlag: true,
                ExpectedPrimaryCpts: new[] { "49406" },
                ExpectedAddOnCpts: new[] { "77012" },
                ExpectedPrimaryIcds: Array.Empty<string>(),
                ForbiddenIcds: Array.Empty<string>()),
            new SampleExpectation(
                "case10_ct_chest_without_contrast.txt",
                Modality: "CT",
                BodyRegions: new[] { "CHEST" },
                Laterality: "NONE",
                ContrastState: "WITHOUT",
                ViewsOrCompleteness: "UNKNOWN",
                GuidanceFlag: false,
                InterventionFlag: false,
                ExpectedPrimaryCpts: new[] { "71250" },
                ExpectedAddOnCpts: Array.Empty<string>(),
                ExpectedPrimaryIcds: Array.Empty<string>(),
                ForbiddenIcds: new[] { "J18.9" })
        };

        foreach (var testCase in cases)
        {
            var reportPath = Path.Combine(sampleRoot, testCase.FileName);
            var reportText = await File.ReadAllTextAsync(reportPath);
            var encounter = extractionService.Extract(testCase.FileName, reportText);

            Assert.Equal(testCase.Modality, encounter.Modality);
            Assert.Equal(testCase.Laterality, encounter.Laterality);
            Assert.Equal(testCase.ContrastState, encounter.ContrastState);
            Assert.Equal(testCase.ViewsOrCompleteness, encounter.ViewsOrCompleteness);
            Assert.Equal(testCase.GuidanceFlag, encounter.GuidanceFlag);
            Assert.Equal(testCase.InterventionFlag, encounter.InterventionFlag);

            foreach (var region in testCase.BodyRegions)
            {
                Assert.Contains(region, encounter.BodyRegions);
            }

            if (!string.Equals(encounter.Modality, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotEmpty(encounter.ModalityEvidenceSpans);
            }

            if (encounter.BodyRegions.Count > 0)
            {
                Assert.NotEmpty(encounter.BodyRegionEvidenceSpans);
            }

            if (!string.Equals(encounter.Laterality, "NONE", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotEmpty(encounter.LateralityEvidenceSpans);
            }

            if (!string.Equals(encounter.ContrastState, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotEmpty(encounter.ContrastEvidenceSpans);
            }

            if (!string.Equals(encounter.ViewsOrCompleteness, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotEmpty(encounter.ViewsOrCompletenessEvidenceSpans);
            }

            if (encounter.GuidanceFlag)
            {
                Assert.NotEmpty(encounter.GuidanceEvidenceSpans);
            }

            if (encounter.InterventionFlag)
            {
                Assert.NotEmpty(encounter.InterventionEvidenceSpans);
            }

            bundlingValidator.Reset();
            var codingResult = await codingService.GenerateAsync(ToCodingEncounter(encounter), CancellationToken.None);
            Assert.True(bundlingValidator.WasInvoked);

            foreach (var expectedCpt in testCase.ExpectedPrimaryCpts)
            {
                Assert.Contains(codingResult.CptResult.PrimaryCpts, item => item.Code == expectedCpt);
            }

            foreach (var expectedAddOn in testCase.ExpectedAddOnCpts)
            {
                Assert.Contains(codingResult.CptResult.AddOnCpts, item => item.Code == expectedAddOn);
            }

            foreach (var expectedIcd in testCase.ExpectedPrimaryIcds)
            {
                Assert.Contains(codingResult.PrimaryCandidates, item => item.Code == expectedIcd);
            }

            foreach (var forbidden in testCase.ForbiddenIcds)
            {
                Assert.DoesNotContain(codingResult.PrimaryCandidates, item => item.Code == forbidden);
                Assert.DoesNotContain(codingResult.SecondaryCandidates, item => item.Code == forbidden);
            }

            if (testCase.FileName == "case07_ct_abd_pelvis_with_contrast.txt")
            {
                var cystConcept = encounter.Concepts.FirstOrDefault(item => item.Text == "cyst");
                Assert.NotNull(cystConcept);
                Assert.Equal("INCIDENTAL", cystConcept!.Relevance);
            }

            if (testCase.FileName == "case01_rule_out_pe.txt")
            {
                var peConcept = encounter.Concepts.FirstOrDefault(item => item.Text == "pulmonary embolism");
                Assert.NotNull(peConcept);
                Assert.Equal("RULED_OUT", peConcept!.Certainty);
            }

            if (testCase.FileName == "case08_mri_shoulder_right_wo.txt")
            {
                var selection = codingResult.CptResult.PrimaryCpts.First(item => item.Code == "73221");
                Assert.Contains("RT", selection.Modifiers);
            }
        }
    }

    private static RadiologyExtractionService CreateExtractionService()
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
        var attributesExtractor = new RadiologyAttributesExtractor();
        var registry = new ConceptPackRegistry();

        return new RadiologyExtractionService(sectionDetector, modalityExtractor, attributesExtractor, registry, extractor, completeness);
    }

    private static (RadiologyCodingService Service, TrackingBundlingValidator Validator) CreateCodingService()
    {
        var handler = new StubTerminologyHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var terminologyClient = new TerminologyClient(httpClient, NullLogger<TerminologyClient>.Instance);
        var safetyGate = new SafetyGate();
        var policy = new RadiologyIcdPolicy();
        var cptService = new RadiologyCptCodingService();
        var bundlingValidator = new TrackingBundlingValidator();
        var codingService = new RadiologyCodingService(
            terminologyClient,
            safetyGate,
            policy,
            cptService,
            bundlingValidator,
            new RulesEngine(Options.Create(new RulesOptions()), Array.Empty<IRuleCategoryValidator>()),
            new ClaimContextBuilder(),
            NullLogger<RadiologyCodingService>.Instance);

        return (codingService, bundlingValidator);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "auto-coding-platform.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private static Coding.Worker.Models.ExtractedRadiologyEncounter ToCodingEncounter(Extraction.Worker.Models.ExtractedRadiologyEncounter encounter)
    {
        return new Coding.Worker.Models.ExtractedRadiologyEncounter
        {
            EncounterId = encounter.EncounterId,
            PayerId = encounter.PayerId,
            DateOfService = encounter.DateOfService,
            ReportText = encounter.ReportText,
            Modality = encounter.Modality,
            BodyRegion = encounter.BodyRegion,
            BodyRegions = encounter.BodyRegions,
            Laterality = encounter.Laterality,
            ContrastState = encounter.ContrastState,
            ViewsOrCompleteness = encounter.ViewsOrCompleteness,
            GuidanceFlag = encounter.GuidanceFlag,
            InterventionFlag = encounter.InterventionFlag,
            BillingContext = encounter.BillingContext,
            IndicationText = encounter.IndicationText,
            IndicationEvidenceSpans = encounter.IndicationEvidenceSpans,
            Sections = encounter.Sections,
            DocumentationCompleteness = new Coding.Worker.Models.DocumentationCompleteness { Score = encounter.DocumentationCompleteness.Score },
            Warnings = encounter.Warnings,
            Concepts = encounter.Concepts.Select(concept => new Coding.Worker.Models.RadiologyConcept
            {
                Text = concept.Text,
                Certainty = concept.Certainty,
                Polarity = concept.Polarity,
                Temporality = concept.Temporality,
                SourcePriority = concept.SourcePriority,
                Relevance = concept.Relevance,
                EvidenceSpans = concept.EvidenceSpans
            }).ToList(),
            ImpressionConcepts = encounter.ImpressionConcepts.Select(concept => new Coding.Worker.Models.RadiologyConcept
            {
                Text = concept.Text,
                Certainty = concept.Certainty,
                Polarity = concept.Polarity,
                Temporality = concept.Temporality,
                SourcePriority = concept.SourcePriority,
                Relevance = concept.Relevance,
                EvidenceSpans = concept.EvidenceSpans
            }).ToList(),
            ModalityEvidenceSpans = encounter.ModalityEvidenceSpans,
            BodyRegionEvidenceSpans = encounter.BodyRegionEvidenceSpans,
            LateralityEvidenceSpans = encounter.LateralityEvidenceSpans,
            ContrastEvidenceSpans = encounter.ContrastEvidenceSpans,
            ViewsOrCompletenessEvidenceSpans = encounter.ViewsOrCompletenessEvidenceSpans,
            GuidanceEvidenceSpans = encounter.GuidanceEvidenceSpans,
            InterventionEvidenceSpans = encounter.InterventionEvidenceSpans
        };
    }

    private sealed record SampleExpectation(
        string FileName,
        string Modality,
        string[] BodyRegions,
        string Laterality,
        string ContrastState,
        string ViewsOrCompleteness,
        bool GuidanceFlag,
        bool InterventionFlag,
        string[] ExpectedPrimaryCpts,
        string[] ExpectedAddOnCpts,
        string[] ExpectedPrimaryIcds,
        string[] ForbiddenIcds);

    private sealed class TrackingBundlingValidator : IBundlingValidator
    {
        public bool WasInvoked { get; private set; }

        public void Reset() => WasInvoked = false;

        public BundlingValidationResult Validate(ExtractedRadiologyEncounter encounter, CptCodingResult cptResult)
        {
            WasInvoked = true;
            return new BundlingValidationResult
            {
                WasValidated = false,
                IsPlaceholder = true,
                ValidatorVersion = "TODO",
                Issues = new List<string>(),
                Notes = new List<string> { "TODO: Implement NCCI/bundling validation rules." }
            };
        }
    }

    private sealed class StubTerminologyHandler : HttpMessageHandler
    {
        private static readonly Dictionary<string, TerminologyHitDto> Hits = new(StringComparer.OrdinalIgnoreCase)
        {
            { "pulmonary embolism", BuildHit("I26.99", "Pulmonary embolism") },
            { "pneumonia", BuildHit("J18.9", "Pneumonia") },
            { "cholelithiasis", BuildHit("K80.20", "Cholelithiasis") },
            { "hydronephrosis", BuildHit("N13.30", "Hydronephrosis") },
            { "fracture", BuildHit("S82.90", "Fracture of lower leg") },
            { "appendicitis", BuildHit("K35.80", "Appendicitis") },
            { "cyst", BuildHit("N28.1", "Renal cyst") },
            { "tumor", BuildHit("D49.9", "Neoplasm of unspecified behavior") }
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await request.Content!.ReadFromJsonAsync<TerminologySearchRequest>(cancellationToken: cancellationToken);
            var responseHits = new List<TerminologyHitDto>();

            if (payload is not null && Hits.TryGetValue(payload.QueryText, out var hit))
            {
                responseHits.Add(hit);
            }

            var json = JsonSerializer.Serialize(responseHits);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private static TerminologyHitDto BuildHit(string code, string description)
        {
            return new TerminologyHitDto
            {
                Code = code,
                ShortDescription = description,
                LongDescription = description,
                Score = 0.90,
                MatchModes = new List<string> { "TEST" },
                MatchedTerms = new List<string> { description }
            };
        }
    }
}
