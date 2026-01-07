namespace Coding.Worker.Services;

public static class RulesOptionsValidator
{
    public static void ValidateOrThrow(RulesOptions options)
    {
        var errors = new List<string>();

        foreach (var pack in options.RulePacks)
        {
            if (string.IsNullOrWhiteSpace(pack.PackId))
            {
                errors.Add("Rule pack PackId is required.");
            }

            if (string.IsNullOrWhiteSpace(pack.Layer))
            {
                errors.Add($"Rule pack {pack.PackId} missing Layer.");
            }
            else if (!IsValidLayer(pack.Layer))
            {
                errors.Add($"Rule pack {pack.PackId} has invalid Layer '{pack.Layer}'.");
            }

            if (pack.EffectiveStart.HasValue && pack.EffectiveEnd.HasValue &&
                pack.EffectiveStart.Value > pack.EffectiveEnd.Value)
            {
                errors.Add($"Rule pack {pack.PackId} has EffectiveStart after EffectiveEnd.");
            }

            var ruleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in pack.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.RuleId))
                {
                    errors.Add($"Rule pack {pack.PackId} contains a rule with empty RuleId.");
                }
                else if (!ruleIds.Add(rule.RuleId))
                {
                    errors.Add($"Rule pack {pack.PackId} contains duplicate RuleId '{rule.RuleId}'.");
                }

                if (!Enum.IsDefined(rule.Category))
                {
                    errors.Add($"Rule pack {pack.PackId} rule {rule.RuleId} has invalid category.");
                }

                if (!Enum.IsDefined(rule.Action.Status))
                {
                    errors.Add($"Rule pack {pack.PackId} rule {rule.RuleId} has invalid status.");
                }

                if (!Enum.IsDefined(rule.Action.Severity))
                {
                    errors.Add($"Rule pack {pack.PackId} rule {rule.RuleId} has invalid severity.");
                }

                if (!Enum.IsDefined(rule.Action.Action))
                {
                    errors.Add($"Rule pack {pack.PackId} rule {rule.RuleId} has invalid action.");
                }

                if (IsEmptyTrigger(rule.Trigger))
                {
                    errors.Add($"Rule pack {pack.PackId} rule {rule.RuleId} has empty trigger.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }
    }

    private static bool IsValidLayer(string layer)
    {
        return layer.Equals("GLOBAL", StringComparison.OrdinalIgnoreCase) ||
               layer.Equals("NCCI_MUE", StringComparison.OrdinalIgnoreCase) ||
               layer.Equals("PAYER", StringComparison.OrdinalIgnoreCase) ||
               layer.Equals("CLIENT", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmptyTrigger(RuleTrigger trigger)
    {
        return trigger.CptCodes.Count == 0 &&
               trigger.IcdCodes.Count == 0 &&
               trigger.IcdPrefixes.Count == 0 &&
               trigger.PlaceOfService.Count == 0 &&
               trigger.MinAge is null &&
               trigger.MaxAge is null &&
               trigger.RequiredModifiers.Count == 0 &&
               trigger.ProviderSpecialties.Count == 0 &&
               !trigger.RequiresDateOfService &&
               !trigger.RequiresIcdMismatch &&
               trigger.MinDaysSinceLast is null;
    }
}
