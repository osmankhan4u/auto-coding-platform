using System.Text.RegularExpressions;
using Extraction.Worker.Models;

namespace Extraction.Worker.Services;

public sealed class SectionDetector
{
    private static readonly Dictionary<string, string> HeadingMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "INDICATION", "Indication" },
        { "REASON FOR EXAM", "Indication" },
        { "CLINICAL HISTORY", "Indication" },
        { "TECHNIQUE", "Technique" },
        { "FINDINGS", "Findings" },
        { "IMPRESSION", "Impression" },
        { "CONCLUSION", "Impression" }
    };

    private static readonly Regex HeadingRegex = new(
        @"^(?<heading>INDICATION|REASON FOR EXAM|CLINICAL HISTORY|TECHNIQUE|FINDINGS|IMPRESSION|CONCLUSION)\s*:?(?<rest>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public SectionDetectionResult Detect(string reportText)
    {
        var result = new SectionDetectionResult();
        var headings = new List<(string Canonical, int HeadingStart, int ContentStart)>();

        foreach (Match match in HeadingRegex.Matches(reportText))
        {
            var headingValue = match.Groups["heading"].Value.Trim();
            if (!HeadingMap.TryGetValue(headingValue, out var canonical))
            {
                continue;
            }

            var restGroup = match.Groups["rest"];
            var contentStart = restGroup.Index;
            if (string.IsNullOrWhiteSpace(restGroup.Value))
            {
                contentStart = GetLineEndIndex(reportText, match.Index) + 1;
                if (contentStart > reportText.Length)
                {
                    contentStart = reportText.Length;
                }
            }

            headings.Add((canonical, match.Index, contentStart));
        }

        headings.Sort((a, b) => a.HeadingStart.CompareTo(b.HeadingStart));

        for (var index = 0; index < headings.Count; index++)
        {
            var heading = headings[index];
            if (result.Sections.ContainsKey(heading.Canonical))
            {
                continue;
            }

            var contentEnd = reportText.Length;
            if (index + 1 < headings.Count)
            {
                contentEnd = headings[index + 1].HeadingStart;
            }

            if (contentEnd < heading.ContentStart)
            {
                contentEnd = heading.ContentStart;
            }

            var contentText = SafeSubstring(reportText, heading.ContentStart, contentEnd - heading.ContentStart);

            result.Sections[heading.Canonical] = new SectionInfo
            {
                Name = heading.Canonical,
                ContentStart = heading.ContentStart,
                ContentEnd = contentEnd,
                ContentText = contentText
            };
        }

        EnsureMissingSection(result, "Indication");
        EnsureMissingSection(result, "Technique");
        EnsureMissingSection(result, "Findings");
        EnsureMissingSection(result, "Impression");

        return result;
    }

    private static void EnsureMissingSection(SectionDetectionResult result, string sectionName)
    {
        if (result.Sections.ContainsKey(sectionName))
        {
            return;
        }

        result.Sections[sectionName] = new SectionInfo
        {
            Name = sectionName,
            ContentStart = -1,
            ContentEnd = -1,
            ContentText = string.Empty
        };
    }

    private static int GetLineEndIndex(string text, int startIndex)
    {
        for (var index = startIndex; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                return index;
            }
        }

        return text.Length;
    }

    private static string SafeSubstring(string text, int start, int length)
    {
        if (start < 0 || start >= text.Length || length <= 0)
        {
            return string.Empty;
        }

        if (start + length > text.Length)
        {
            length = text.Length - start;
        }

        return text.Substring(start, length);
    }
}
