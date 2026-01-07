using Extraction.Worker.Models;

namespace Extraction.Worker.Services;

public sealed class ClinicalConceptExtractor
{
    private readonly SentenceSplitter _sentenceSplitter;
    private readonly NegationScopeResolver _negationResolver;
    private readonly UncertaintyScopeResolver _uncertaintyResolver;
    private readonly HistoryScopeResolver _historyResolver;
    private readonly TargetAwareNegationResolver _targetAwareNegationResolver;

    public ClinicalConceptExtractor(
        SentenceSplitter sentenceSplitter,
        NegationScopeResolver negationResolver,
        UncertaintyScopeResolver uncertaintyResolver,
        HistoryScopeResolver historyResolver,
        TargetAwareNegationResolver targetAwareNegationResolver)
    {
        _sentenceSplitter = sentenceSplitter;
        _negationResolver = negationResolver;
        _uncertaintyResolver = uncertaintyResolver;
        _historyResolver = historyResolver;
        _targetAwareNegationResolver = targetAwareNegationResolver;
    }

    public List<RadiologyConcept> Extract(
        string reportText,
        IReadOnlyDictionary<string, SectionInfo> sections,
        IReadOnlyCollection<ConceptPattern> patterns)
    {
        var concepts = new List<RadiologyConcept>();
        var indicationConcepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasIndicationText = sections.TryGetValue("Indication", out var indicationSection) &&
                                !string.IsNullOrWhiteSpace(indicationSection.ContentText);

        foreach (var sectionName in new[] { "Indication", "Findings", "Impression" })
        {
            if (!sections.TryGetValue(sectionName, out var section) ||
                string.IsNullOrWhiteSpace(section.ContentText))
            {
                continue;
            }

            var sentences = _sentenceSplitter.Split(section.ContentText);
            foreach (var sentence in sentences)
            {
                foreach (var pattern in patterns)
                {
                    foreach (System.Text.RegularExpressions.Match match in pattern.Regex.Matches(sentence.Text))
                    {
                        if (!match.Success)
                        {
                            continue;
                        }

                        var negated = _negationResolver.IsNegated(sentence.Text, match.Index) ||
                                      _targetAwareNegationResolver.IsNegated(sentence.Text, match.Value, match.Index);
                        var uncertain = _uncertaintyResolver.IsUncertain(sentence.Text, match.Index);
                        var historical = _historyResolver.IsHistorical(sentence.Text, match.Index);

                        var certainty = negated ? "RULED_OUT" : uncertain ? "SUSPECTED" : "CONFIRMED";
                        var polarity = negated ? "NEGATIVE" : "POSITIVE";
                        var temporality = historical ? "HISTORY" : "CURRENT";

                        var absoluteStart = section.ContentStart < 0
                            ? match.Index
                            : section.ContentStart + sentence.Start + match.Index;
                        var absoluteEnd = absoluteStart + match.Length;

                        var concept = new RadiologyConcept
                        {
                            Text = pattern.Normalized,
                            Certainty = certainty,
                            Polarity = polarity,
                            Temporality = temporality,
                            SourcePriority = MapSourcePriority(sectionName),
                            Relevance = "UNCLEAR",
                            EvidenceSpans = new List<string>
                            {
                                $"{sectionName}:{absoluteStart}-{absoluteEnd}"
                            }
                        };

                        if (sectionName == "Indication" && !negated)
                        {
                            indicationConcepts.Add(pattern.Normalized);
                        }

                        concepts.Add(concept);
                    }
                }
            }
        }

        foreach (var concept in concepts)
        {
            if (concept.SourcePriority == "INDICATION")
            {
                concept.Relevance = "INDICATION_RELATED";
                continue;
            }

            if (indicationConcepts.Count == 0)
            {
                if (hasIndicationText &&
                    string.Equals(concept.SourcePriority, "FINDINGS", StringComparison.OrdinalIgnoreCase))
                {
                    concept.Relevance = "INCIDENTAL";
                }
                else
                {
                    concept.Relevance = "UNCLEAR";
                }
            }
            else if (indicationConcepts.Contains(concept.Text))
            {
                concept.Relevance = "INDICATION_RELATED";
            }
            else
            {
                concept.Relevance = "INCIDENTAL";
            }
        }

        return concepts;
    }

    private static string MapSourcePriority(string sectionName) =>
        sectionName switch
        {
            "Indication" => "INDICATION",
            "Impression" => "IMPRESSION",
            _ => "FINDINGS"
        };
}
