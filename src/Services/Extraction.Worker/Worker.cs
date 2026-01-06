using System.Text.Json;
using Extraction.Worker.Models;
using Extraction.Worker.Services;

namespace Extraction.Worker;

public sealed class Worker : BackgroundService
{
    private static readonly JsonSerializerOptions OutputJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly ILogger<Worker> _logger;
    private readonly RadiologyExtractionService _extractionService;
    private readonly IHostEnvironment _hostEnvironment;

    public Worker(
        ILogger<Worker> logger,
        RadiologyExtractionService extractionService,
        IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _extractionService = extractionService;
        _hostEnvironment = hostEnvironment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var inputDirectory = Path.Combine(_hostEnvironment.ContentRootPath, "samples", "_in");
        var outputDirectory = Path.Combine(_hostEnvironment.ContentRootPath, "samples", "_out");

        Directory.CreateDirectory(outputDirectory);

        if (!Directory.Exists(inputDirectory))
        {
            _logger.LogWarning("Input directory {InputDirectory} does not exist.", inputDirectory);
            return;
        }

        foreach (var inputFile in Directory.EnumerateFiles(inputDirectory, "*.txt"))
        {
            try
            {
                var reportText = await File.ReadAllTextAsync(inputFile, stoppingToken);
                var encounterId = Path.GetFileNameWithoutExtension(inputFile);

                var encounter = _extractionService.Extract(encounterId, reportText);

                var outputPath = Path.Combine(outputDirectory, $"{encounter.EncounterId}.extracted.json");
                var outputJson = JsonSerializer.Serialize(encounter, OutputJsonOptions);
                await File.WriteAllTextAsync(outputPath, outputJson, stoppingToken);

                _logger.LogInformation("Processed {InputFile} -> {OutputFile}", inputFile, outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process {InputFile}", inputFile);
            }
        }
    }
}
