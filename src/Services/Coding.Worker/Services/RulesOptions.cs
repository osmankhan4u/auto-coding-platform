using Coding.Worker.Contracts;

namespace Coding.Worker.Services;

public sealed class RulesOptions
{
    public List<RulePackDefinition> RulePacks { get; set; } = new();
}

public sealed class RulePackDefinition
{
    public string PackId { get; set; } = string.Empty;
    public string PayerId { get; set; } = "DEFAULT";
    public string PlanId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateOnly? EffectiveStart { get; set; }
    public DateOnly? EffectiveEnd { get; set; }
    public int Priority { get; set; }
    public string Layer { get; set; } = "PAYER";
    public List<RuleDefinition> Rules { get; set; } = new();
}

public sealed class RuleDefinition
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleVersion { get; set; } = "1.0";
    public int Priority { get; set; }
    public RuleCategory Category { get; set; } = RuleCategory.Integrity;
    public RuleTrigger Trigger { get; set; } = new();
    public RuleAction Action { get; set; } = new();
    public RuleEvidenceRequirement EvidenceRequirement { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public sealed class RuleTrigger
{
    public List<string> CptCodes { get; set; } = new();
    public List<string> IcdCodes { get; set; } = new();
    public List<string> IcdPrefixes { get; set; } = new();
    public List<string> PlaceOfService { get; set; } = new();
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public List<string> RequiredModifiers { get; set; } = new();
    public List<string> ProviderSpecialties { get; set; } = new();
    public bool RequiresDateOfService { get; set; }
    public bool RequiresIcdMismatch { get; set; }
    public int? MinDaysSinceLast { get; set; }
}

public sealed class RuleAction
{
    public RuleStatus Status { get; set; } = RuleStatus.Fail;
    public RuleSeverity Severity { get; set; } = RuleSeverity.Blocking;
    public RuleActionType Action { get; set; } = RuleActionType.RequestInfo;
}

public sealed class RuleEvidenceRequirement
{
    public List<string> RequiredEvidenceSources { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
