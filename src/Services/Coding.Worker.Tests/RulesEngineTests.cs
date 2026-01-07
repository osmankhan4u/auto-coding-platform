using Coding.Worker.Models;
using Coding.Worker.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Coding.Worker.Tests;

public sealed class RulesEngineTests
{
    [Fact]
    public void Evaluate_ReturnsNoPacksWhenNoneConfigured()
    {
        var engine = new RulesEngine(Options.Create(new RulesOptions()));
        var result = engine.Evaluate(new ClaimContext());

        Assert.Equal("PASS", result.Status);
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
                            Trigger = new RuleTrigger
                            {
                                CptCodes = new List<string> { "71260" },
                                IcdPrefixes = new List<string> { "R07" }
                            },
                            Action = new RuleAction
                            {
                                Status = "PASS",
                                Severity = "NON_BLOCKING",
                                Action = "auto_release"
                            },
                            Message = "CT chest supported."
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options);
        var claim = new ClaimContext
        {
            Header = new ClaimHeader { PayerId = "DEFAULT" },
            Procedures = new List<ProcedureEntry> { new() { Code = "71260" } },
            Diagnoses = new List<DiagnosisEntry> { new() { Code = "R07.9" } }
        };

        var result = engine.Evaluate(claim);

        Assert.Single(result.Outcomes);
        Assert.Equal("PASS", result.Outcomes[0].Status);
        Assert.Equal("auto_release", result.Outcomes[0].Action);
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
                            Trigger = new RuleTrigger { CptCodes = new List<string> { "71260" } },
                            Action = new RuleAction { Status = "WARN", Severity = "NON_BLOCKING", Action = "route_predicted" }
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options);
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
                            Trigger = new RuleTrigger
                            {
                                CptCodes = new List<string> { "71260" },
                                RequiresDateOfService = true
                            },
                            Action = new RuleAction
                            {
                                Status = "NEEDS_INFO",
                                Severity = "BLOCKING",
                                Action = "request_info"
                            }
                        }
                    }
                }
            }
        });

        var engine = new RulesEngine(options);
        var claim = new ClaimContext
        {
            Header = new ClaimHeader { PayerId = "DEFAULT" },
            Procedures = new List<ProcedureEntry> { new() { Code = "71260" } }
        };

        var result = engine.Evaluate(claim);

        Assert.Single(result.Outcomes);
        Assert.Equal("NEEDS_INFO", result.Outcomes[0].Status);
        Assert.Equal("request_info", result.Outcomes[0].Action);
    }
}
