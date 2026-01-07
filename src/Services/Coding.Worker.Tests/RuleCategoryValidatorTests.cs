using Coding.Worker.Contracts;
using Coding.Worker.Models;
using Coding.Worker.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Coding.Worker.Tests;

public sealed class RuleCategoryValidatorTests
{
    [Fact]
    public void AuthValidator_RequiresEvidence()
    {
        var options = Options.Create(new RulesOptions
        {
            RulePacks = new List<RulePackDefinition>
            {
                new()
                {
                    PackId = "AUTH-PACK",
                    PayerId = "DEFAULT",
                    Layer = "PAYER",
                    Rules = new List<RuleDefinition>
                    {
                        new()
                        {
                            RuleId = "AUTH_REQUIRED",
                            Category = RuleCategory.Auth,
                            Trigger = new RuleTrigger { CptCodes = new List<string> { "71260" } },
                            EvidenceRequirement = new RuleEvidenceRequirement
                            {
                                RequiredEvidenceSources = new List<string> { "Auth" }
                            }
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options, new IRuleCategoryValidator[]
        {
            new AuthRuleValidator()
        });

        var claim = new ClaimContext
        {
            Header = new ClaimHeader
            {
                PayerId = "DEFAULT",
                DateOfService = new DateOnly(2025, 1, 1),
                RenderingProviderNpi = "1111111111",
                BillingProviderNpi = "2222222222",
                PlaceOfService = ""
            },
            Procedures = new List<ProcedureEntry> { new() { Code = "71260" } }
        };

        var result = engine.Evaluate(claim);

        Assert.Contains(result.Outcomes, outcome =>
            outcome.RuleId == "AUTH_REQUIRED" &&
            outcome.Status == RuleStatus.NeedsInfo &&
            outcome.Action == RuleActionType.RequestInfo);
    }

    [Fact]
    public void FrequencyValidator_RoutesPredicted()
    {
        var options = Options.Create(new RulesOptions
        {
            RulePacks = new List<RulePackDefinition>
            {
                new()
                {
                    PackId = "FREQ-PACK",
                    PayerId = "DEFAULT",
                    Layer = "PAYER",
                    Rules = new List<RuleDefinition>
                    {
                        new()
                        {
                            RuleId = "FREQ_CHECK",
                            Category = RuleCategory.Frequency,
                            Trigger = new RuleTrigger { CptCodes = new List<string> { "71260" } }
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options, new IRuleCategoryValidator[]
        {
            new FrequencyRuleValidator(new FakeUtilizationHistoryService())
        });

        var claim = new ClaimContext
        {
            Header = new ClaimHeader { PayerId = "DEFAULT", DateOfService = new DateOnly(2025, 1, 1) },
            Patient = new PatientContext { PatientId = "PAT-1" },
            Procedures = new List<ProcedureEntry> { new() { Code = "71260" } }
        };

        var result = engine.Evaluate(claim);

        Assert.Contains(result.Outcomes, outcome =>
            outcome.RuleId == "FREQ_CHECK" &&
            outcome.Status == RuleStatus.NeedsInfo &&
            outcome.Action == RuleActionType.RoutePredicted);
    }

    [Fact]
    public void PosSpecialtyValidator_RequiresPos()
    {
        var options = Options.Create(new RulesOptions
        {
            RulePacks = new List<RulePackDefinition>
            {
                new()
                {
                    PackId = "POS-PACK",
                    PayerId = "DEFAULT",
                    Layer = "PAYER",
                    Rules = new List<RuleDefinition>
                    {
                        new()
                        {
                            RuleId = "POS_CHECK",
                            Category = RuleCategory.PosSpecialty,
                            Trigger = new RuleTrigger
                            {
                                CptCodes = new List<string> { "71260" },
                                PlaceOfService = new List<string> { "11" }
                            }
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options, new IRuleCategoryValidator[]
        {
            new PosSpecialtyRuleValidator()
        });

        var claim = new ClaimContext
        {
            Header = new ClaimHeader { PayerId = "DEFAULT", DateOfService = new DateOnly(2025, 1, 1) },
            Procedures = new List<ProcedureEntry> { new() { Code = "71260" } }
        };

        var result = engine.Evaluate(claim);

        Assert.Contains(result.Outcomes, outcome =>
            outcome.RuleId == "POS_CHECK" &&
            outcome.Status == RuleStatus.NeedsInfo &&
            outcome.Action == RuleActionType.RequestInfo);
    }
}
