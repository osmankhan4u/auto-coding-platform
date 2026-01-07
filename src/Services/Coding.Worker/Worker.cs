using System.Text.Json;
using Coding.Worker.Models;
using Coding.Worker.Services;

namespace Coding.Worker;

public sealed class Worker : BackgroundService
{
    private static readonly JsonSerializerOptions InputJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions OutputJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly ILogger<Worker> _logger;
    private readonly RadiologyCodingService _codingService;
    private readonly IHostEnvironment _hostEnvironment;

    public Worker(
        ILogger<Worker> logger,
        RadiologyCodingService codingService,
        IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _codingService = codingService;
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

        foreach (var inputFile in Directory.EnumerateFiles(inputDirectory, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(inputFile, stoppingToken);
                var encounter = JsonSerializer.Deserialize<ExtractedRadiologyEncounter>(json, InputJsonOptions);
                if (encounter is null)
                {
                    throw new InvalidOperationException("Failed to deserialize ExtractedRadiologyEncounter.");
                }

                var result = await _codingService.GenerateAsync(encounter, stoppingToken);

                var outputPath = Path.Combine(outputDirectory, $"{encounter.EncounterId}.icd.json");
                var outputJson = JsonSerializer.Serialize(result, OutputJsonOptions);
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
