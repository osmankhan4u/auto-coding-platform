namespace Extraction.Worker.Services;

public sealed class NegationScopeResolver
{
    private static readonly string[] PreNegationCues =
    {
        "no",
        "denies",
        "without",
        "negative for",
        "free of",
        "absence of",
        "not"
    };

    private static readonly string[] PostNegationCues =
    {
        "not seen",
        "not identified",
        "not demonstrated",
        "not visualized",
        "absent"
    };

    private static readonly string[] ScopeTerminators =
    {
        "but",
        "however",
        "except",
        "although",
        "yet"
    };

    private static readonly string[] PositiveClauseCues =
    {
        "is present",
        "are present",
        "is seen",
        "are seen",
        "was seen"
    };

    public bool IsNegated(string sentenceText, int matchIndex)
    {
        if (string.IsNullOrWhiteSpace(sentenceText) || matchIndex < 0 || matchIndex >= sentenceText.Length)
        {
            return false;
        }

        var lower = sentenceText.ToLowerInvariant();
        var terminatorAfterMatch = FindTerminatorAfter(lower, matchIndex);

        foreach (var cue in PostNegationCues)
        {
            var cueIndex = lower.IndexOf(cue, matchIndex, StringComparison.Ordinal);
            if (cueIndex >= 0 && cueIndex < terminatorAfterMatch)
            {
                return true;
            }
        }

        var preCueIndex = -1;
        var preCueLength = 0;
        foreach (var cue in PreNegationCues)
        {
            var cueIndex = FindLastCueIndex(lower, cue, matchIndex);
            if (cueIndex >= 0 && cueIndex > preCueIndex)
            {
                preCueIndex = cueIndex;
                preCueLength = cue.Length;
            }
        }

        if (preCueIndex < 0)
        {
            return false;
        }

        if (HasTerminatorBetween(lower, preCueIndex + preCueLength, matchIndex))
        {
            return false;
        }

        if (PositiveCueBlocksScope(lower, preCueIndex + preCueLength, matchIndex))
        {
            return false;
        }

        return true;
    }

    private static int FindTerminatorAfter(string text, int startIndex)
    {
        var earliest = text.Length;

        for (var index = startIndex; index < text.Length; index++)
        {
            var value = text[index];
            if (value == '.' || value == ';' || value == ':')
            {
                earliest = index;
                break;
            }
        }

        foreach (var terminator in ScopeTerminators)
        {
            var index = IndexOfWord(text, terminator, startIndex, text.Length);
            if (index >= 0 && index < earliest)
            {
                earliest = index;
            }
        }

        return earliest;
    }

    private static bool HasTerminatorBetween(string text, int start, int end)
    {
        for (var index = start; index < end && index < text.Length; index++)
        {
            var value = text[index];
            if (value == '.' || value == ';' || value == ':')
            {
                return true;
            }
        }

        foreach (var terminator in ScopeTerminators)
        {
            if (IndexOfWord(text, terminator, start, end) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PositiveCueBlocksScope(string text, int start, int end)
    {
        for (var index = start; index < end; index++)
        {
            if (text[index] != ',')
            {
                continue;
            }

            var commaIndex = index + 1;
            foreach (var cue in PositiveClauseCues)
            {
                var cueIndex = text.IndexOf(cue, commaIndex, StringComparison.Ordinal);
                if (cueIndex >= commaIndex && cueIndex < end)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int IndexOfWord(string text, string word, int start, int end)
    {
        var index = text.IndexOf(word, start, StringComparison.Ordinal);
        while (index >= 0 && index < end)
        {
            var before = index == 0 ? ' ' : text[index - 1];
            var afterIndex = index + word.Length;
            var after = afterIndex >= text.Length ? ' ' : text[afterIndex];

            if (!char.IsLetterOrDigit(before) && !char.IsLetterOrDigit(after))
            {
                return index;
            }

            index = text.IndexOf(word, index + 1, StringComparison.Ordinal);
        }

        return -1;
    }

    private static int FindLastCueIndex(string text, string cue, int matchIndex)
    {
        var index = text.LastIndexOf(cue, matchIndex, StringComparison.Ordinal);
        while (index >= 0)
        {
            if (IsPhraseBoundary(text, cue, index))
            {
                return index;
            }

            index = text.LastIndexOf(cue, index - 1, StringComparison.Ordinal);
        }

        return -1;
    }

    private static bool IsPhraseBoundary(string text, string cue, int index)
    {
        var before = index == 0 ? ' ' : text[index - 1];
        var afterIndex = index + cue.Length;
        var after = afterIndex >= text.Length ? ' ' : text[afterIndex];
        return !char.IsLetterOrDigit(before) && !char.IsLetterOrDigit(after);
    }
}
