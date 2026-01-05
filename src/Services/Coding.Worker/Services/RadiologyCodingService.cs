using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class RadiologyCodingService
{
    private const double PrimaryScoreThreshold = 0.72;
    private const int TerminologyTopN = 10;
    private readonly TerminologyClient _terminologyClient;
    private readonly SafetyGate _safetyGate;
    private readonly ILogger<RadiologyCodingService> _logger;

    public RadiologyCodingService(
        TerminologyClient terminologyClient,
        SafetyGate safetyGate,
        ILogger<RadiologyCodingService> logger)
    {
        _terminologyClient = terminologyClient;
        _safetyGate = safetyGate;
        _logger = logger;
    }

    public async Task<RadiologyIcdCodingResult> GenerateAsync(
        ExtractedRadiologyEncounter encounter,
        CancellationToken cancellationToken)
    {
        var trace = new DecisionTrace
        {
            PolicyDecisions =
            {
                "Primary candidates restricted to INDICATION concepts.",
                "Secondary candidates are suggestions only.",
                "Safety gate blocks auto primary when required."
            }
        };

        var safetyResult = _safetyGate.Evaluate(encounter);
        var primaryConcepts = encounter.Concepts
            .Where(concept => IsIndicationConcept(concept) && !IsExcludedConcept(concept))
            .ToList();

        var secondaryConcepts = encounter.Concepts
            .Where(concept => !IsIndicationConcept(concept) && !IsExcludedConcept(concept))
            .ToList();

        var primaryCandidates = await BuildCandidatesAsync(primaryConcepts, trace, cancellationToken, applySuspectedPenalty: true);
        var secondaryCandidates = await BuildCandidatesAsync(secondaryConcepts, trace, cancellationToken, applySuspectedPenalty: false);

        var finalSelection = new IcdFinalSelection
        {
            PrimaryIcd = null,
            SecondaryIcds = new List<IcdCandidate>(),
            RequiresHumanReview = true
        };

        if (safetyResult.CanAutoSelect && primaryCandidates.Count > 0)
        {
            var topCandidate = primaryCandidates.OrderByDescending(candidate => candidate.Score).First();
            if (topCandidate.Score >= PrimaryScoreThreshold)
            {
                finalSelection.PrimaryIcd = topCandidate;
                finalSelection.RequiresHumanReview = false;
            }
        }

        if (!safetyResult.CanAutoSelect)
        {
            _logger.LogInformation("Safety gate blocked auto primary selection. Flags: {Flags}", safetyResult.Flags);
        }

        return new RadiologyIcdCodingResult
        {
            PrimaryCandidates = primaryCandidates,
            SecondaryCandidates = secondaryCandidates,
            FinalSelection = finalSelection,
            SafetyFlags = safetyResult.Flags,
            Trace = trace
        };
    }

    private async Task<List<IcdCandidate>> BuildCandidatesAsync(
        IReadOnlyCollection<RadiologyConcept> concepts,
        DecisionTrace trace,
        CancellationToken cancellationToken,
        bool applySuspectedPenalty)
    {
        var candidateByCode = new Dictionary<string, IcdCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var concept in concepts)
        {
            if (string.IsNullOrWhiteSpace(concept.Text))
            {
                continue;
            }

            var hits = await _terminologyClient.SearchAsync(concept.Text, TerminologyTopN, cancellationToken);
            trace.TerminologyQueries.Add(new TerminologyQueryTrace
            {
                QueryText = concept.Text,
                TopN = TerminologyTopN,
                ResultCount = hits.Count
            });

            foreach (var hit in hits)
            {
                var score = applySuspectedPenalty && IsSuspected(concept)
                    ? Math.Max(0, hit.Score - 0.05)
                    : hit.Score;

                if (!candidateByCode.TryGetValue(hit.Code, out var existing) || score > existing.Score)
                {
                    candidateByCode[hit.Code] = new IcdCandidate
                    {
                        Code = hit.Code,
                        ShortDescription = hit.ShortDescription,
                        LongDescription = hit.LongDescription,
                        Score = score,
                        MatchModes = hit.MatchModes,
                        MatchedTerms = hit.MatchedTerms,
                        EvidenceSpans = concept.EvidenceSpans
                    };
                }
            }
        }

        return candidateByCode.Values
            .OrderByDescending(candidate => candidate.Score)
            .ToList();
    }

    private static bool IsIndicationConcept(RadiologyConcept concept) =>
        string.Equals(concept.SourcePriority, "INDICATION", StringComparison.OrdinalIgnoreCase);

    private static bool IsExcludedConcept(RadiologyConcept concept) =>
        string.Equals(concept.Certainty, "RULED_OUT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(concept.Polarity, "NEGATIVE", StringComparison.OrdinalIgnoreCase);

    private static bool IsSuspected(RadiologyConcept concept) =>
        string.Equals(concept.Certainty, "SUSPECTED", StringComparison.OrdinalIgnoreCase);
}
