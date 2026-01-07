using Coding.Worker.Models;
using Coding.Worker.Services;
using Coding.Worker.Contracts;
using Microsoft.Extensions.Options;
using Xunit;

namespace Coding.Worker.Tests;

public sealed class RulesEngineTests
{
    [Fact]
    public void Evaluate_ReturnsNoPacksWhenNoneConfigured()
    {
        var engine = new RulesEngine(Options.Create(new RulesOptions()), Array.Empty<IRuleCategoryValidator>());
        var result = engine.Evaluate(new ClaimContext
        {
            Header = new ClaimHeader
            {
                DateOfService = new DateOnly(2025, 1, 1),
                PlaceOfService = "11",
                RenderingProviderNpi = "1111111111",
                BillingProviderNpi = "2222222222"
            }
        });

        Assert.Equal(RuleStatus.Pass, result.Status);
        Assert.Contains("No rule packs applicable.", result.Notes);
    }

    [Fact]
    public void Evaluate_FiresMatchingRule()
    {
        var options = Options.Create(new RulesOptions
        {
            RulePacks = new List<RulePackDefinition>
            {
                new()
                {
                    PackId = "PAYER-DEFAULT",
                    PayerId = "DEFAULT",
                    Layer = "PAYER",
                    Rules = new List<RuleDefinition>
                    {
                        new()
                        {
                            RuleId = "CT_CHEST_REQUIRE_DX",
                            RuleVersion = "1.0",
                            Category = RuleCategory.MedicalNecessity,
                            Trigger = new RuleTrigger
                            {
                                CptCodes = new List<string> { "71260" },
                                IcdPrefixes = new List<string> { "R07" }
                            },
                            Action = new RuleAction
                            {
                                Status = RuleStatus.Pass,
                                Severity = RuleSeverity.NonBlocking,
                                Action = RuleActionType.AutoRelease
                            },
                            Message = "CT chest supported."
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options, Array.Empty<IRuleCategoryValidator>());
        var claim = new ClaimContext
        {
            Header = new ClaimHeader
            {
                PayerId = "DEFAULT",
                DateOfService = new DateOnly(2025, 1, 1),
                PlaceOfService = "11",
                RenderingProviderNpi = "1111111111",
                BillingProviderNpi = "2222222222"
            },
            Procedures = new List<ProcedureEntry> { new() { Code = "71260" } },
            Diagnoses = new List<DiagnosisEntry> { new() { Code = "R07.9" } }
        };

        var result = engine.Evaluate(claim);

        Assert.Contains(result.Outcomes, outcome => outcome.Action == RuleActionType.AutoRelease);
    }

    [Fact]
    public void Evaluate_RespectsEffectiveDates()
    {
        var options = Options.Create(new RulesOptions
        {
            RulePacks = new List<RulePackDefinition>
            {
                new()
                {
                    PackId = "ACTIVE-PACK",
                    PayerId = "DEFAULT",
                    EffectiveStart = new DateOnly(2025, 1, 1),
                    EffectiveEnd = new DateOnly(2025, 12, 31),
                    Rules = new List<RuleDefinition>
                    {
                        new()
                        {
                            RuleId = "ACTIVE",
                            Category = RuleCategory.MedicalNecessity,
                            Trigger = new RuleTrigger { CptCodes = new List<string> { "71260" } },
                            Action = new RuleAction { Status = RuleStatus.Warn, Severity = RuleSeverity.NonBlocking, Action = RuleActionType.RoutePredicted }
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options, Array.Empty<IRuleCategoryValidator>());
        var claim = new ClaimContext
        {
            Header = new ClaimHeader { PayerId = "DEFAULT", DateOfService = new DateOnly(2024, 6, 1) },
            Procedures = new List<ProcedureEntry> { new() { Code = "71260" } }
        };

        var result = engine.Evaluate(claim);

        Assert.Contains("No rule packs applicable.", result.Notes);
    }

    [Fact]
    public void Evaluate_TriggersWhenDateOfServiceMissing()
    {
        var options = Options.Create(new RulesOptions
        {
            RulePacks = new List<RulePackDefinition>
            {
                new()
                {
                    PackId = "GLOBAL-DOS",
                    PayerId = "DEFAULT",
                    Layer = "GLOBAL",
                    Rules = new List<RuleDefinition>
                    {
                        new()
                        {
                            RuleId = "REQ_DOS",
                            Category = RuleCategory.Integrity,
                            Trigger = new RuleTrigger
                            {
                                CptCodes = new List<string> { "71260" },
                                RequiresDateOfService = true
                            },
                            Action = new RuleAction
                            {
                                Status = RuleStatus.NeedsInfo,
                                Severity = RuleSeverity.Blocking,
                                Action = RuleActionType.RequestInfo
                            }
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options, Array.Empty<IRuleCategoryValidator>());
        var claim = new ClaimContext
        {
            Header = new ClaimHeader { PayerId = "DEFAULT" },
            Procedures = new List<ProcedureEntry> { new() { Code = "71260" } }
        };

        var result = engine.Evaluate(claim);

        Assert.True(result.Outcomes.Count >= 1);
        Assert.Contains(result.Outcomes, outcome =>
            outcome.Status == RuleStatus.NeedsInfo &&
            outcome.Action == RuleActionType.RequestInfo &&
            outcome.Layer == "GLOBAL");
    }

    [Fact]
    public void Evaluate_FlagsMissingRequiredHeaderFields()
    {
        var engine = new RulesEngine(Options.Create(new RulesOptions()), Array.Empty<IRuleCategoryValidator>());
        var claim = new ClaimContext
        {
            Header = new ClaimHeader
            {
                PayerId = string.Empty,
                PlaceOfService = string.Empty,
                RenderingProviderNpi = string.Empty,
                BillingProviderNpi = string.Empty
            }
        };

        var result = engine.Evaluate(claim);

        Assert.Contains(result.Outcomes, outcome => outcome.RuleId == "GLOBAL_MISSING_PAYER");
        Assert.Contains(result.Outcomes, outcome => outcome.RuleId == "GLOBAL_MISSING_POS");
        Assert.Contains(result.Outcomes, outcome => outcome.RuleId == "GLOBAL_MISSING_RENDERING_NPI");
        Assert.Contains(result.Outcomes, outcome => outcome.RuleId == "GLOBAL_MISSING_BILLING_NPI");
    }

    [Fact]
    public void Evaluate_FlagsInvalidModifierFormat()
    {
        var engine = new RulesEngine(Options.Create(new RulesOptions()), Array.Empty<IRuleCategoryValidator>());
        var claim = new ClaimContext
        {
            Header = new ClaimHeader { DateOfService = new DateOnly(2025, 1, 1) },
            Procedures = new List<ProcedureEntry>
            {
                new()
                {
                    Code = "71260",
                    Modifiers = new List<string> { "2", "ABC" }
                }
            }
        };

        var result = engine.Evaluate(claim);

        Assert.Contains(result.Outcomes, outcome => outcome.RuleId == "GLOBAL_INVALID_MODIFIER");
    }
}
