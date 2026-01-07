namespace Coding.Worker.Services;

public interface IUtilizationHistoryService
{
    bool TryGetMostRecentProcedureDate(string patientId, string procedureCode, out DateOnly lastDate);
}
