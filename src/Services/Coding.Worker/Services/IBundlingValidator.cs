using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public interface IBundlingValidator
{
    BundlingValidationResult Validate(ExtractedRadiologyEncounter encounter, CptCodingResult cptResult);
}
