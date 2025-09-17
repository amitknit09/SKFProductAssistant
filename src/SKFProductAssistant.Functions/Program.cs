// Program.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SKFProductAssistant.Application.Behaviors;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Application.Queries;
using SKFProductAssistant.Application.Services;
using SKFProductAssistant.Functions.Extensions;
using SKFProductAssistant.Infrastructure.Repositories;
using SKFProductAssistant.Infrastructure.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add application services
        services.AddApplicationServices(context.Configuration);
    })
    .Build();

host.Run();
