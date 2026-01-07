using Coding.Worker;
using Coding.Worker.Services;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TerminologyOptions>(builder.Configuration.GetSection("Terminology"));
builder.Services.Configure<RulesOptions>(builder.Configuration.GetSection("Rules"));

builder.Services.AddSingleton<SafetyGate>();
builder.Services.AddSingleton<RadiologyIcdPolicy>();
builder.Services.AddSingleton<RadiologyCptCodingService>();
builder.Services.AddSingleton<IBundlingValidator, BundlingValidator>();
builder.Services.AddSingleton<IUtilizationHistoryService, FakeUtilizationHistoryService>();
builder.Services.AddSingleton<IRuleCategoryValidator, AuthRuleValidator>();
builder.Services.AddSingleton<IRuleCategoryValidator, FrequencyRuleValidator>();
builder.Services.AddSingleton<IRuleCategoryValidator, PosSpecialtyRuleValidator>();
builder.Services.AddSingleton<IRulesEngine, RulesEngine>();
builder.Services.AddSingleton<ClaimContextBuilder>();
builder.Services.AddSingleton<RadiologyCodingService>();
builder.Services.AddHttpClient<TerminologyClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<TerminologyOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl);
    }
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
