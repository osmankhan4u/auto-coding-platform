using Coding.Worker.Contracts;
using Coding.Worker.Models;
using Coding.Worker.Services;
using Xunit;

namespace Coding.Worker.Tests;

public sealed class BundlingValidatorTests
{
    [Fact]
    public void Validate_FlagsDuplicateCodes()
    {
        var validator = new BundlingValidator();
        var result = validator.Validate(
            new ExtractedRadiologyEncounter(),
            new CptCodingResult
            {
                PrimaryCpts = new List<CptCodeSelection>
                {
                    new() { Code = "71260" },
                    new() { Code = "71260" }
                }
            });

        Assert.True(result.WasValidated);
        Assert.Contains("DUPLICATE_CPT:71260", result.Issues);
    }

    [Fact]
    public void Validate_FlagsGuidanceBundledWithPrimary()
    {
        var validator = new BundlingValidator();
        var cptResult = new CptCodingResult
        {
            PrimaryCpts = new List<CptCodeSelection>
            {
                new() { Code = "49406" }
            },
            AddOnCpts = new List<CptCodeSelection>
            {
                new() { Code = "77012" }
            }
        };

        var result = validator.Validate(new ExtractedRadiologyEncounter(), cptResult);

        Assert.Contains(result.Issues, item => item.StartsWith("GUIDANCE_BUNDLED_WITH:49406:77012"));
        Assert.Contains("BUNDLED_WITH_PRIMARY", cptResult.AddOnCpts[0].ExclusionReasons);
        Assert.True(cptResult.RequiresHumanReview);
    }
}
