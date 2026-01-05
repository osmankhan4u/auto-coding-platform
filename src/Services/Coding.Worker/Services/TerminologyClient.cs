using System.Net.Http.Json;

namespace Coding.Worker.Services;

public sealed class TerminologyClient
{
    private const string CodeSystem = "ICD10CM";
    private const string CodeVersionId = "ICD10CM_2026";
    private readonly HttpClient _httpClient;
    private readonly ILogger<TerminologyClient> _logger;

    public TerminologyClient(HttpClient httpClient, ILogger<TerminologyClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<TerminologyHitDto>> SearchAsync(string queryText, int topN, CancellationToken cancellationToken)
    {
        var request = new TerminologySearchRequest
        {
            CodeSystem = CodeSystem,
            CodeVersionId = CodeVersionId,
            QueryText = queryText,
            TopN = topN
        };

        var response = await _httpClient.PostAsJsonAsync("/terminology/search", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Terminology search failed with status {StatusCode} for query {QueryText}.", response.StatusCode, queryText);
            return new List<TerminologyHitDto>();
        }

        var results = await response.Content.ReadFromJsonAsync<List<TerminologyHitDto>>(cancellationToken: cancellationToken);
        return results ?? new List<TerminologyHitDto>();
    }
}

public sealed class TerminologySearchRequest
{
    public string CodeSystem { get; set; } = string.Empty;
    public string CodeVersionId { get; set; } = string.Empty;
    public DateOnly? DateOfService { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public int TopN { get; set; } = 10;
    public string? IsBillableOnly { get; set; }
    public string? ExcludeHeaders { get; set; }
}

public sealed class TerminologyHitDto
{
    public string Code { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> MatchModes { get; set; } = new();
    public List<string> MatchedTerms { get; set; } = new();
}
