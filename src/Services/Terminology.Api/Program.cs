using Microsoft.EntityFrameworkCore;
using Terminology.Api.Data;
using Terminology.Api.Models;
using Terminology.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TerminologyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TerminologyDb")));

builder.Services.AddSingleton<IEmbeddingProvider, FakeEmbeddingProvider>();
builder.Services.AddScoped<TerminologySearchService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("OK"));

app.MapPost("/terminology/search", async (
    TerminologySearchRequest request,
    TerminologySearchService service,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.CodeVersionId))
    {
        return Results.BadRequest("codeVersionId is required.");
    }

    if (!Guid.TryParse(request.CodeVersionId, out var codeVersionId))
    {
        return Results.BadRequest("codeVersionId must be a UUID.");
    }

    if (string.IsNullOrWhiteSpace(request.QueryText))
    {
        return Results.BadRequest("queryText is required.");
    }

    var results = await service.SearchAsync(request, codeVersionId, cancellationToken);
    return Results.Ok(results);
});

app.Run();
