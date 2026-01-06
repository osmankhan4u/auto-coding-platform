using Extraction.Worker;
using Extraction.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<SectionDetector>();
builder.Services.AddSingleton<SentenceSplitter>();
builder.Services.AddSingleton<NegationScopeResolver>();
builder.Services.AddSingleton<UncertaintyScopeResolver>();
builder.Services.AddSingleton<HistoryScopeResolver>();
builder.Services.AddSingleton<TargetAwareNegationResolver>();
builder.Services.AddSingleton<ConceptPackRegistry>();
builder.Services.AddSingleton<DocumentationCompletenessScorer>();
builder.Services.AddSingleton<ClinicalConceptExtractor>();
builder.Services.AddSingleton<ModalityBodyRegionExtractor>();
builder.Services.AddSingleton<RadiologyExtractionService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
