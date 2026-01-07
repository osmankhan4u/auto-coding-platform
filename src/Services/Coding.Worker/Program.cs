using Coding.Worker;
using Coding.Worker.Services;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TerminologyOptions>(builder.Configuration.GetSection("Terminology"));

builder.Services.AddSingleton<SafetyGate>();
builder.Services.AddSingleton<RadiologyIcdPolicy>();
builder.Services.AddSingleton<RadiologyCptCodingService>();
builder.Services.AddSingleton<IBundlingValidator, BundlingValidator>();
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
