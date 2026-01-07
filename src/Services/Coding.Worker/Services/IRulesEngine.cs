using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public interface IRulesEngine
{
    RuleEvaluationResult Evaluate(ClaimContext claim);
}
