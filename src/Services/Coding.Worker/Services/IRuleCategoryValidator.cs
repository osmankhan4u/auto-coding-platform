using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public interface IRuleCategoryValidator
{
    RuleCategory Category { get; }
    RuleOutcome Validate(RuleDefinition rule, RuleOutcome outcome, ClaimContext claim);
}
