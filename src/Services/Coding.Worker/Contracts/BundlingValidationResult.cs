namespace Coding.Worker.Contracts;

public sealed class BundlingValidationResult
{
    public bool WasValidated { get; set; }
    public bool IsPlaceholder { get; set; }
    public string ValidatorVersion { get; set; } = "TODO";
    public List<string> Issues { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}
