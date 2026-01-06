using System.IO.Compression;
using System.Xml;

namespace Terminology.Loader.Pipeline;

public sealed class Icd10CmIndexParser
{
    public IEnumerable<AliasRow> ParseAliases(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.Entries.FirstOrDefault(e =>
            e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
            e.FullName.Contains("index", StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            yield break;
        }

        using var stream = entry.Open();
        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true
        };

        using var reader = XmlReader.Create(stream, settings);
        var stack = new Stack<TermNode>();
        string? currentElement = null;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                currentElement = reader.Name;
                if (reader.Name == "mainTerm" || reader.Name == "term")
                {
                    stack.Push(new TermNode());
                }
            }
            else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
            {
                if (stack.Count == 0)
                {
                    continue;
                }

                var text = reader.Value?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var node = stack.Peek();
                if (currentElement == "title" && string.IsNullOrWhiteSpace(node.Title))
                {
                    node.Title = text;
                }
                else if (currentElement == "code")
                {
                    node.PendingCode = text;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.Name == "code" && stack.Count > 0)
                {
                    var node = stack.Peek();
                    var code = node.PendingCode;
                    node.PendingCode = null;

                    var aliasText = BuildAliasText(stack);
                    if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(aliasText))
                    {
                        yield return new AliasRow(code.Trim(), aliasText);
                    }
                }

                if (reader.Name == "mainTerm" || reader.Name == "term")
                {
                    if (stack.Count > 0)
                    {
                        stack.Pop();
                    }
                }

                currentElement = null;
            }
        }
    }

    private static string BuildAliasText(IEnumerable<TermNode> nodes)
    {
        var titles = nodes.Reverse()
            .Select(node => node.Title?.Trim())
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .ToArray();

        return titles.Length == 0 ? string.Empty : string.Join(" ", titles);
    }

    private sealed class TermNode
    {
        public string? Title { get; set; }
        public string? PendingCode { get; set; }
    }
}
