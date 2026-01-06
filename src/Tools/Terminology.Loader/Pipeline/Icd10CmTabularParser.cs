using System.IO.Compression;
using System.Xml;

namespace Terminology.Loader.Pipeline;

public sealed class Icd10CmTabularParser
{
    public IEnumerable<ConceptRow> ParseConcepts(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.Entries.FirstOrDefault(e =>
            e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
            e.FullName.Contains("tabular", StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            throw new InvalidOperationException("Tabular XML not found in input zip.");
        }

        using var stream = entry.Open();
        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true
        };

        using var reader = XmlReader.Create(stream, settings);
        var stack = new Stack<DiagNode>();
        string? currentElement = null;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                currentElement = reader.Name;
                if (reader.Name == "diag")
                {
                    if (stack.Count > 0)
                    {
                        stack.Peek().HasChild = true;
                    }

                    stack.Push(new DiagNode());
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
                if (currentElement == "name" && string.IsNullOrWhiteSpace(node.Code))
                {
                    node.Code = text;
                }
                else if (currentElement == "desc" && string.IsNullOrWhiteSpace(node.ShortDesc))
                {
                    node.ShortDesc = text;
                }
                else if (currentElement == "longdesc" && string.IsNullOrWhiteSpace(node.LongDesc))
                {
                    node.LongDesc = text;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.Name == "diag" && stack.Count > 0)
                {
                    var node = stack.Pop();
                    if (string.IsNullOrWhiteSpace(node.Code))
                    {
                        currentElement = null;
                        continue;
                    }

                    var shortDesc = string.IsNullOrWhiteSpace(node.ShortDesc)
                        ? node.LongDesc ?? string.Empty
                        : node.ShortDesc;
                    var longDesc = string.IsNullOrWhiteSpace(node.LongDesc)
                        ? shortDesc
                        : node.LongDesc;

                    var isHeader = node.HasChild;
                    yield return new ConceptRow(
                        node.Code.Trim(),
                        shortDesc.Trim(),
                        longDesc.Trim(),
                        isHeader,
                        !isHeader);
                }

                currentElement = null;
            }
        }
    }

    private sealed class DiagNode
    {
        public string? Code { get; set; }
        public string? ShortDesc { get; set; }
        public string? LongDesc { get; set; }
        public bool HasChild { get; set; }
    }
}
