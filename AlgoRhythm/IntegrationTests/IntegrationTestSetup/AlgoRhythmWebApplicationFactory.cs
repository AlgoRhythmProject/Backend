using AlgoRhythm.Clients;
using AlgoRhythm.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.IntegrationTestSetup;

internal class AlgoRhythmWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly WebApplicationFactory<CodeExecutor.Program> _serviceFactory;
    public AlgoRhythmWebApplicationFactory(WebApplicationFactory<CodeExecutor.Program> serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("JWT_KEY", TestConstants.TestJwtKey);

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.Sources.Clear();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Jwt:Issuer", TestConstants.TestJwtIssuer},
                {"Jwt:Audience", TestConstants.TestJwtAudience},
                {"SendGrid:ApiKey", TestConstants.TestSendGridApiKey},
                {"SendGrid:FromName", TestConstants.TestSendGridFromName},
                {"SendGrid:FromEmail", TestConstants.TestSendGridFromEmail},
            });
        });

        builder.ConfigureServices(services =>
        {
            // Delete existing db context
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // In-Memory Database
            var dbName = "TestDb_" + Guid.NewGuid();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.EnableSensitiveDataLogging(); 
            });

           
            services.AddSingleton(sp =>
            {
                var http = _serviceFactory.CreateClient();
                var logger = sp.GetRequiredService<ILogger<CodeExecutorClient>>();
                return new CodeExecutorClient(http, logger);
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });
    }
}
