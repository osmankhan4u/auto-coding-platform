using System.Text.RegularExpressions;

namespace Extraction.Worker.Services;

public sealed class TargetAwareNegationResolver
{
    public bool IsNegated(string sentenceText, string matchText, int matchIndex)
    {
        if (string.IsNullOrWhiteSpace(sentenceText) || string.IsNullOrWhiteSpace(matchText))
        {
            return false;
        }

        var targetPattern = BuildTargetPattern(matchText);

        var prePattern = $@"\b(no|negative for)\b\s+(?:\w+\s+){{0,3}}{targetPattern}";
        if (Regex.IsMatch(sentenceText, prePattern, RegexOptions.IgnoreCase))
        {
            return true;
        }

        var postPattern = $@"{targetPattern}\s+(?:is\s+)?(not seen|not identified|not demonstrated|not visualized|absent)\b";
        if (Regex.IsMatch(sentenceText, postPattern, RegexOptions.IgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string BuildTargetPattern(string matchText)
    {
        var escaped = Regex.Escape(matchText.Trim());
        return escaped.Replace("\\ ", "\\s+");
    }
}
