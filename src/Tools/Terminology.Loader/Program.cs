using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Terminology.Data;
using Terminology.Loader;
using Terminology.Loader.Pipeline;
using Terminology.Loader.Services;
using Terminology.Data.Services;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!LoaderOptions.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine("Example: --codeSystem ICD10CM --codeVersionId ICD10CM_2026 --inputZip <path> --modelId fake-embed-1536 --embed false --aliases true");
            return 1;
        }

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("TerminologyDb");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.Error.WriteLine("ConnectionStrings:TerminologyDb is missing.");
                return 1;
            }

            var dbOptions = new DbContextOptionsBuilder<TerminologyDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            await using var dbContext = new TerminologyDbContext(dbOptions);

            var embeddingProvider = new FakeBackedEmbeddingProvider(options.ModelId);
            var orchestrator = new LoaderOrchestrator(
                dbContext,
                options,
                new Icd10CmTabularParser(),
                new Icd10CmIndexParser(),
                embeddingProvider);

            await orchestrator.RunAsync(CancellationToken.None);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
