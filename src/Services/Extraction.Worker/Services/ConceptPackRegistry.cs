using System.Text.RegularExpressions;
using Extraction.Worker.Models;

namespace Extraction.Worker.Services;

public sealed class ConceptPackRegistry
{
    private readonly List<(string Name, List<ConceptPattern> Patterns)> _packs;

    public ConceptPackRegistry()
        : this(BuildDefaultPacks())
    {
    }

    public ConceptPackRegistry(IEnumerable<(string Name, List<ConceptPattern> Patterns)> packs)
    {
        _packs = packs.ToList();
    }

    public ConceptPackResolution Resolve(string? modality, string? bodyRegion)
    {
        var applied = new List<string> { "GLOBAL" };
        var resolved = new List<ConceptPattern>();

        var normalizedModality = (modality ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedRegion = (bodyRegion ?? string.Empty).Trim().ToUpperInvariant();

        if (normalizedModality == "CT")
        {
            applied.Add("CT_COMMON");
            if (normalizedRegion == "CHEST")
            {
                applied.Add("CT_CHEST");
            }
            else if (normalizedRegion == "ABDOMEN")
            {
                applied.Add("CT_ABDOMEN");
            }
            else if (normalizedRegion == "ABD_PELVIS")
            {
                applied.Add("CT_ABD_PELVIS");
            }
        }
        else if (normalizedModality == "MRI")
        {
            applied.Add("MRI_COMMON");
            if (normalizedRegion == "BRAIN_HEAD")
            {
                applied.Add("MRI_BRAIN");
            }
        }
        else if (normalizedModality == "US")
        {
            applied.Add("US_COMMON");
            if (normalizedRegion == "ABDOMEN")
            {
                applied.Add("US_ABDOMEN");
            }
        }

        var patternMap = new Dictionary<string, ConceptPattern>(StringComparer.OrdinalIgnoreCase);

        foreach (var packName in applied)
        {
            var pack = _packs.FirstOrDefault(p => string.Equals(p.Name, packName, StringComparison.OrdinalIgnoreCase));
            if (pack.Patterns is null)
            {
                continue;
            }

            foreach (var pattern in pack.Patterns)
            {
                var key = $"{pattern.Type}|{pattern.Normalized}";
                if (!patternMap.ContainsKey(key))
                {
                    patternMap[key] = pattern;
                }
            }
        }

        resolved.AddRange(patternMap.Values);

        return new ConceptPackResolution
        {
            Patterns = resolved,
            AppliedPacks = applied
        };
    }

    private static IEnumerable<(string Name, List<ConceptPattern> Patterns)> BuildDefaultPacks()
    {
        return new List<(string Name, List<ConceptPattern> Patterns)>
        {
            ("GLOBAL", new List<ConceptPattern>
            {
                BuildPattern("fracture", "Condition", @"\bfracture\b"),
                BuildPattern("mass", "Condition", @"\bmass\b"),
                BuildPattern("cyst", "Condition", @"\bcyst\b"),
                BuildPattern("lesion", "Condition", @"\blesion\b")
            }),
            ("CT_COMMON", new List<ConceptPattern>
            {
                BuildPattern("pulmonary embolism", "Condition", @"\b(?:pulmonary\s+embolism|pe)\b"),
                BuildPattern("pneumothorax", "Condition", @"\bpneumothorax\b"),
                BuildPattern("pneumonia", "Condition", @"\bpneumonia\b")
            }),
            ("MRI_COMMON", new List<ConceptPattern>
            {
                BuildPattern("infarct", "Condition", @"\binfarct\b"),
                BuildPattern("ischemia", "Condition", @"\bischemia\b"),
                BuildPattern("tumor", "Condition", @"\btumou?r\b")
            }),
            ("US_COMMON", new List<ConceptPattern>
            {
                BuildPattern("cholelithiasis", "Condition", @"\b(?:cholelithiasis|gallstones?)\b"),
                BuildPattern("hydronephrosis", "Condition", @"\bhydronephrosis\b")
            }),
            ("CT_CHEST", new List<ConceptPattern>
            {
                BuildPattern("pulmonary embolism", "Condition", @"\b(?:pulmonary\s+embolism|pe)\b"),
                BuildPattern("pneumothorax", "Condition", @"\bpneumothorax\b"),
                BuildPattern("pneumonia", "Condition", @"\bpneumonia\b")
            }),
            ("CT_ABDOMEN", new List<ConceptPattern>
            {
                BuildPattern("appendicitis", "Condition", @"\bappendicitis\b"),
                BuildPattern("bowel obstruction", "Condition", @"\b(?:bowel\s+obstruction|obstruction)\b")
            }),
            ("CT_ABD_PELVIS", new List<ConceptPattern>
            {
                BuildPattern("appendicitis", "Condition", @"\bappendicitis\b"),
                BuildPattern("diverticulitis", "Condition", @"\bdiverticulitis\b"),
                BuildPattern("bowel obstruction", "Condition", @"\b(?:bowel\s+obstruction|obstruction)\b")
            }),
            ("MRI_BRAIN", new List<ConceptPattern>
            {
                BuildPattern("stroke", "Condition", @"\b(?:stroke|cva)\b"),
                BuildPattern("intracranial hemorrhage", "Condition", @"\b(?:intracranial\s+hemorrhage|hemorrhage)\b")
            }),
            ("US_ABDOMEN", new List<ConceptPattern>
            {
                BuildPattern("cholelithiasis", "Condition", @"\b(?:cholelithiasis|gallstones?)\b"),
                BuildPattern("hydronephrosis", "Condition", @"\bhydronephrosis\b")
            })
        };
    }

    private static ConceptPattern BuildPattern(string normalized, string type, string regexPattern)
    {
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        return new ConceptPattern(normalized, type, regex);
    }
}
