using System.Text.Json;
using Coding.Worker.Contracts;
using Coding.Worker.Models;
using Coding.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace RadiologyBestPracticeVerificationTests;

public sealed class RulesEngineGoldenClaimsTests
{
    [Fact]
    public void GoldenClaim_CtChest_MatchesExpectedOutcome()
    {
        var basePath = FindRepoRoot();
        var claimPath = Path.Combine(basePath, "tests", "rules", "golden_claim_ct_chest.json");
        var expectedPath = Path.Combine(basePath, "tests", "rules", "expected_outcome_ct_chest.json");
        var claimJson = File.ReadAllText(claimPath);
        var expectedJson = File.ReadAllText(expectedPath);

        var claim = JsonSerializer.Deserialize<ClaimContext>(claimJson, JsonOptions())!;
        var expected = JsonSerializer.Deserialize<RuleEvaluationResult>(expectedJson, JsonOptions())!;

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(Path.Combine("src", "Services", "Coding.Worker", "appsettings.json"), optional: false)
            .Build();

        var rulesOptions = new RulesOptions();
        config.GetSection("Rules").Bind(rulesOptions);
        var engine = new RulesEngine(Options.Create(rulesOptions), Array.Empty<IRuleCategoryValidator>());

        var result = engine.Evaluate(claim);

        Assert.Equal(expected.Status, result.Status);
        Assert.Equal(expected.Severity, result.Severity);
        Assert.Equal(expected.Actions, result.Actions);
        if (expected.WinningRule is null)
        {
            Assert.Null(result.WinningRule);
        }
        else
        {
            Assert.NotNull(result.WinningRule);
            Assert.Equal(expected.WinningRule.RuleId, result.WinningRule!.RuleId);
        }
    }

    [Theory]
    [InlineData("golden_claim_us_abdomen.json", "expected_outcome_us_abdomen.json")]
    [InlineData("golden_claim_xr_knee.json", "expected_outcome_xr_knee.json")]
    [InlineData("golden_claim_ir_guidance.json", "expected_outcome_ir_guidance.json")]
    [InlineData("golden_claim_ct_abd_pelvis.json", "expected_outcome_ct_abd_pelvis.json")]
    [InlineData("golden_claim_ct_chest_denial.json", "expected_outcome_ct_chest_denial.json")]
    public void GoldenClaims_OtherCases_MatchExpectedOutcome(string claimFile, string expectedFile)
    {
        var basePath = FindRepoRoot();
        var claimPath = Path.Combine(basePath, "tests", "rules", claimFile);
        var expectedPath = Path.Combine(basePath, "tests", "rules", expectedFile);
        var claimJson = File.ReadAllText(claimPath);
        var expectedJson = File.ReadAllText(expectedPath);

        var claim = JsonSerializer.Deserialize<ClaimContext>(claimJson, JsonOptions())!;
        var expected = JsonSerializer.Deserialize<RuleEvaluationResult>(expectedJson, JsonOptions())!;

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(Path.Combine("src", "Services", "Coding.Worker", "appsettings.json"), optional: false)
            .Build();

        var rulesOptions = new RulesOptions();
        config.GetSection("Rules").Bind(rulesOptions);
        var engine = new RulesEngine(Options.Create(rulesOptions), Array.Empty<IRuleCategoryValidator>());

        var result = engine.Evaluate(claim);

        Assert.Equal(expected.Status, result.Status);
        Assert.Equal(expected.Severity, result.Severity);
        Assert.Equal(expected.Actions, result.Actions);
        if (expected.WinningRule is null)
        {
            Assert.Null(result.WinningRule);
        }
        else
        {
            Assert.NotNull(result.WinningRule);
            Assert.Equal(expected.WinningRule.RuleId, result.WinningRule!.RuleId);
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "auto-coding-platform.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }
}
