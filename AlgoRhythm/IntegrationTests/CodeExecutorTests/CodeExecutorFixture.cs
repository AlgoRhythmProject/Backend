using AlgoRhythm.Clients;
using Microsoft.AspNetCore.Mvc.Testing;
using AlgoRhythm;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.CodeExecutorTests
{
    public class CodeExecutorFixture : IDisposable
    {
        internal WebApplicationFactory<Program> ServerFactory { get; }
        private readonly WebApplicationFactory<CodeExecutor.Program> _serviceFactory;

        public CodeExecutorFixture()
        {
            // CodeExecutor service
            _serviceFactory = new WebApplicationFactory<CodeExecutor.Program>();
            var codeExecutorHttpClient = _serviceFactory.CreateClient();
            var codeExecutorBaseUrl = codeExecutorHttpClient.BaseAddress!.ToString();

            // Start the main server and configure it to use the test CodeExecutor service
            ServerFactory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Remove the production CodeExecutorClient registration
                        var descriptor = services.SingleOrDefault(d =>
                            d.ServiceType == typeof(CodeExecutorClient));

                        if (descriptor != null)
                            services.Remove(descriptor);

                        // Register CodeExecutorClient using the test server's HttpClient
                        services.AddSingleton(sp =>
                        {
                            var http = _serviceFactory.CreateClient(); // <- TestServer client
                            return new CodeExecutorClient(http);
                        });
                    });

                });
        }

        public void Dispose()
        {
            _serviceFactory.Dispose();
            ServerFactory.Dispose();
        }
    }
}