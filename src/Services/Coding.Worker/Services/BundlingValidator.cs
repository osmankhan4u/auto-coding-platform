using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class BundlingValidator : IBundlingValidator
{
    public BundlingValidationResult Validate(ExtractedRadiologyEncounter encounter, CptCodingResult cptResult)
    {
        return new BundlingValidationResult
        {
            WasValidated = false,
            IsPlaceholder = true,
            ValidatorVersion = "TODO",
            Issues = new List<string>(),
            Notes = new List<string>
            {
                "TODO: Implement NCCI/bundling validation rules."
            }
        };
    }
}
