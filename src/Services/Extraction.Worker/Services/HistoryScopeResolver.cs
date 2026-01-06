namespace Extraction.Worker.Services;

public sealed class HistoryScopeResolver
{
    private static readonly string[] Cues =
    {
        "history of",
        "prior",
        "previous",
        "old",
        "chronic",
        "status post",
        "s/p",
        "known"
    };

    private static readonly string[] ScopeTerminators =
    {
        "but",
        "however",
        "except",
        "although",
        "yet"
    };

    public bool IsHistorical(string sentenceText, int matchIndex)
    {
        if (string.IsNullOrWhiteSpace(sentenceText) || matchIndex < 0 || matchIndex >= sentenceText.Length)
        {
            return false;
        }

        var lower = sentenceText.ToLowerInvariant();
        var cueIndex = -1;
        var cueLength = 0;

        foreach (var cue in Cues)
        {
            var index = FindLastCueIndex(lower, cue, matchIndex);
            if (index >= 0 && index > cueIndex)
            {
                cueIndex = index;
                cueLength = cue.Length;
            }
        }

        if (cueIndex < 0)
        {
            return false;
        }

        if (HasTerminatorBetween(lower, cueIndex + cueLength, matchIndex))
        {
            return false;
        }

        return true;
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
