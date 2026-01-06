namespace Extraction.Worker.Models;

public sealed class Sentence
{
    public string Text { get; init; } = string.Empty;
    public int Start { get; init; }
    public int End { get; init; }
}
