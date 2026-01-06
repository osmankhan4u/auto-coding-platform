using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class SectionDetectorTests
{
    [Fact]
    public void Detect_HandlesCommonFormats()
    {
        var detector = new SectionDetector();
        var samples = new[]
        {
            new
            {
                Text = "INDICATION: Chest pain\nTECHNIQUE: CT CHEST W CONTRAST\nFINDINGS: No PE.\nIMPRESSION: Normal.",
                Indication = "Chest pain",
                Technique = "CT CHEST W CONTRAST",
                Findings = "No PE.",
                Impression = "Normal."
            },
            new
            {
                Text = "REASON FOR EXAM\nChest pain\nFINDINGS:\nNo acute process.\nCONCLUSION:\nNormal.",
                Indication = "Chest pain",
                Technique = "",
                Findings = "No acute process.",
                Impression = "Normal."
            },
            new
            {
                Text = "CLINICAL HISTORY: Headache\nTECHNIQUE\nMRI BRAIN\nFINDINGS\nNo hemorrhage.\nIMPRESSION\nNormal.",
                Indication = "Headache",
                Technique = "MRI BRAIN",
                Findings = "No hemorrhage.",
                Impression = "Normal."
            },
            new
            {
                Text = "Indication: Fever\nTechnique: CT Abdomen\nFindings: Appendix normal.\nImpression: Negative.",
                Indication = "Fever",
                Technique = "CT Abdomen",
                Findings = "Appendix normal.",
                Impression = "Negative."
            },
            new
            {
                Text = "INDICATION\nShortness of breath\nFINDINGS\nLungs clear.\nIMPRESSION\nNormal.",
                Indication = "Shortness of breath",
                Technique = "",
                Findings = "Lungs clear.",
                Impression = "Normal."
            },
            new
            {
                Text = "TECHNIQUE: MRI BRAIN\nFINDINGS: No infarct.\nIMPRESSION: Normal.",
                Indication = "",
                Technique = "MRI BRAIN",
                Findings = "No infarct.",
                Impression = "Normal."
            },
            new
            {
                Text = "INDICATION: Abd pain\nFINDINGS: Appendicitis.\nCONCLUSION: Appendicitis.",
                Indication = "Abd pain",
                Technique = "",
                Findings = "Appendicitis.",
                Impression = "Appendicitis."
            },
            new
            {
                Text = "INDICATION :  Trauma\nTECHNIQUE :  CT CHEST\nFINDINGS :  Rib fracture.\nIMPRESSION :  Rib fracture.",
                Indication = "Trauma",
                Technique = "CT CHEST",
                Findings = "Rib fracture.",
                Impression = "Rib fracture."
            }
        };

        foreach (var sample in samples)
        {
            var result = detector.Detect(sample.Text);
            Assert.Equal(sample.Indication, result.Sections["Indication"].ContentText.Trim());
            Assert.Equal(sample.Technique, result.Sections["Technique"].ContentText.Trim());
            Assert.Equal(sample.Findings, result.Sections["Findings"].ContentText.Trim());
            Assert.Equal(sample.Impression, result.Sections["Impression"].ContentText.Trim());
        }
    }

    [Fact]
    public void Detect_PreservesSpanIndices()
    {
        var detector = new SectionDetector();
        var text = "INDICATION: Chest pain\nFINDINGS: No PE.\nIMPRESSION: Normal.";
        var result = detector.Detect(text);

        var indication = result.Sections["Indication"];
        var substring = text.Substring(indication.ContentStart, indication.ContentEnd - indication.ContentStart);
        Assert.Equal(indication.ContentText, substring);
    }
}
