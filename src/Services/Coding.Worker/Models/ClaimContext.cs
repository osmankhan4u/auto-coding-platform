namespace Coding.Worker.Models;

public sealed class ClaimContext
{
    public ClaimHeader Header { get; set; } = new();
    public PatientContext Patient { get; set; } = new();
    public EncounterContext Encounter { get; set; } = new();
    public List<DiagnosisEntry> Diagnoses { get; set; } = new();
    public List<ProcedureEntry> Procedures { get; set; } = new();
    public List<SupportingEvidence> Evidence { get; set; } = new();
}

public sealed class ClaimHeader
{
    public string PayerId { get; set; } = "DEFAULT";
    public string PlanId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PlaceOfService { get; set; } = string.Empty;
    public DateOnly? DateOfService { get; set; }
    public string RenderingProviderNpi { get; set; } = string.Empty;
    public string BillingProviderNpi { get; set; } = string.Empty;
    public string OrderingProviderNpi { get; set; } = string.Empty;
    public string RenderingProviderSpecialty { get; set; } = string.Empty;
}

public sealed class PatientContext
{
    public int? Age { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string EligibilityStatus { get; set; } = string.Empty;
}

public sealed class EncounterContext
{
    public string VisitType { get; set; } = string.Empty;
    public string FacilityId { get; set; } = string.Empty;
    public string ReferrerId { get; set; } = string.Empty;
    public DateOnly? AdmitDate { get; set; }
    public DateOnly? DischargeDate { get; set; }
}

public sealed class DiagnosisEntry
{
    public string Code { get; set; } = string.Empty;
    public bool IsPrincipal { get; set; }
    public string PresentOnAdmission { get; set; } = string.Empty;
}

public sealed class ProcedureEntry
{
    public string Code { get; set; } = string.Empty;
    public int Units { get; set; } = 1;
    public List<string> Modifiers { get; set; } = new();
    public string Laterality { get; set; } = string.Empty;
}

public sealed class SupportingEvidence
{
    public string EvidenceId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
}
