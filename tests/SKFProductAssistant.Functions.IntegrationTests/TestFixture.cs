using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Functions.Extensions;
using SKFProductAssistant.Functions.Functions;
using SKFProductAssistant.Functions.IntegrationTests.TestHelpers;
using System.Text;
using System.Text.Json;

namespace SKFProductAssistant.Functions.IntegrationTests
{
    public class TestFixture : IDisposable
    {
        private readonly IHost _host;
        private readonly IServiceScope _scope;
        private readonly string _testDataPath;

        public TestFixture()
        {
            _testDataPath = CreateTestDataDirectoryWithRealFiles();
      
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AzureOpenAI:Endpoint"] = Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint")
            ?? "https://skf-openai-dev-eval.openai.azure.com/",
                    ["AzureOpenAI:ApiKey"] = Environment.GetEnvironmentVariable("AzureOpenAI__ApiKey") ?? "test-key",
                    ["AzureOpenAI:DeploymentName"] = "gpt-4o-mini",
                    ["AzureOpenAI:ModelName"] = "gpt-4o-mini",
                    ["AzureOpenAI:ApiVersion"] = "2024-08-01-preview",
                    ["AzureOpenAI:MaxTokens"] = "4000",
                    ["AzureOpenAI:Temperature"] = "0.7",
                    ["DataPath"] = _testDataPath
                })
                .Build();

            _host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddApplicationServices(configuration);
                    services.AddScoped<ProductQueryFunction>();
                    services.AddLogging(builder => builder.AddConsole());
                })
                .Build();

            _scope = _host.Services.CreateScope();
        }

        public T GetService<T>() where T : notnull
        {
            return _scope.ServiceProvider.GetRequiredService<T>();
        }

        public HttpRequestData CreateHttpRequestData(string method, string path, object? body = null)
        {
            // Use the mock-based approach for simplicity
            var functionContext = MockFunctionContext.Create(_scope.ServiceProvider);

            string? bodyJson = null;
            if (body != null)
            {
                bodyJson = JsonSerializer.Serialize(body);
            }

            var request = new TestHttpRequestData(functionContext, method, $"http://localhost:7071{path}", bodyJson);

            if (bodyJson != null)
            {
                request.Headers.Add("Content-Type", "application/json");
            }

            return request;
        }

        public async Task<string> ReadResponseContent(HttpResponseData response)
        {
            if (response is TestHttpResponseData testResponse)
            {
                return testResponse.GetBodyAsString();
            }

            response.Body.Position = 0;
            using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        private string CreateTestDataDirectoryWithRealFiles()
        {
            var testDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDataPath);

            // Create test data for your SKF bearing files
            CreateTestDataFiles(testDataPath);

            return testDataPath;
        }

        private void CreateTestDataFiles(string testDataPath)
        {
            // Create test data for 6205 bearing
            var testData6205 = new[]
            {
                new
                {
                    ProductName = "6205",
                    InnerDiameter = "25mm",
                    OuterDiameter = "52mm",
                    Width = "15mm",
                    DynamicLoadRating = "14000N",
                    StaticLoadRating = "6900N",
                    LimitingSpeed = "19000rpm",
                    Mass = "0.128kg"
                }
            };

            // Create test data for 6025-N bearing
            var testData6025N = new[]
            {
                new
                {
                    ProductName = "6025-N",
                    InnerDiameter = "125mm",
                    OuterDiameter = "190mm",
                    Width = "32mm",
                    DynamicLoadRating = "71500N",
                    StaticLoadRating = "56000N",
                    LimitingSpeed = "4300rpm",
                    Mass = "2.65kg"
                }
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

            File.WriteAllText(
                Path.Combine(testDataPath, "6205.json"),
                JsonSerializer.Serialize(testData6205, jsonOptions)
            );

            File.WriteAllText(
                Path.Combine(testDataPath, "6025-N.json"),
                JsonSerializer.Serialize(testData6025N, jsonOptions)
            );
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _host?.Dispose();

            if (Directory.Exists(_testDataPath))
            {
                try
                {
                    Directory.Delete(_testDataPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
