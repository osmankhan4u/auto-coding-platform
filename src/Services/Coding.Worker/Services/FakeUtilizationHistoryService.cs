namespace Coding.Worker.Services;

public sealed class FakeUtilizationHistoryService : IUtilizationHistoryService
{
    public bool TryGetMostRecentProcedureDate(string patientId, string procedureCode, out DateOnly lastDate)
    {
        lastDate = default;
        return false;
    }
}
