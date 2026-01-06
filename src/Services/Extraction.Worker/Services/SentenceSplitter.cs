using Extraction.Worker.Models;

namespace Extraction.Worker.Services;

public sealed class SentenceSplitter
{
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "dr.",
        "vs.",
        "mr.",
        "mrs.",
        "ms.",
        "e.g.",
        "i.e."
    };

    public IReadOnlyList<Sentence> Split(string text)
    {
        var sentences = new List<Sentence>();
        var start = 0;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (!IsTerminator(current))
            {
                continue;
            }

            if (current == '.' && IsAbbreviation(text, index))
            {
                continue;
            }

            var end = index + 1;
            if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                end = index + 2;
                index++;
            }

            AddSentence(text, start, end, sentences);
            start = end;
        }

        AddSentence(text, start, text.Length, sentences);
        return sentences;
    }

    private static bool IsTerminator(char value) =>
        value == '.' || value == ';' || value == ':' || value == '\n' || value == '\r';

    private static bool IsAbbreviation(string text, int periodIndex)
    {
        var start = periodIndex - 1;
        while (start >= 0 && !char.IsWhiteSpace(text[start]))
        {
            start--;
        }

        var tokenStart = start + 1;
        var tokenLength = periodIndex - tokenStart + 1;
        if (tokenLength <= 1)
        {
            return false;
        }

        var token = text.Substring(tokenStart, tokenLength).Trim();
        return Abbreviations.Contains(token);
    }

    private static void AddSentence(string text, int start, int end, List<Sentence> sentences)
    {
        if (end <= start)
        {
            return;
        }

        var trimmedStart = start;
        while (trimmedStart < end && char.IsWhiteSpace(text[trimmedStart]))
        {
            trimmedStart++;
        }

        var trimmedEnd = end;
        while (trimmedEnd > trimmedStart && char.IsWhiteSpace(text[trimmedEnd - 1]))
        {
            trimmedEnd--;
        }

        if (trimmedEnd <= trimmedStart)
        {
            return;
        }

        sentences.Add(new Sentence
        {
            Start = trimmedStart,
            End = trimmedEnd,
            Text = text.Substring(trimmedStart, trimmedEnd - trimmedStart)
        });
    }
}
