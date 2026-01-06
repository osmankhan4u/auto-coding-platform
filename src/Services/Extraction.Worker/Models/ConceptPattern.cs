using System.Text.RegularExpressions;

namespace Extraction.Worker.Models;

public sealed class ConceptPattern
{
    public ConceptPattern(string normalized, string type, Regex regex)
    {
        Normalized = normalized;
        Type = type;
        Regex = regex;
    }

    public string Normalized { get; }
    public string Type { get; }
    public Regex Regex { get; }
}
