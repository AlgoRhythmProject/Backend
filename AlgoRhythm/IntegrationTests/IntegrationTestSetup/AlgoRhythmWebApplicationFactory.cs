using AlgoRhythm.Clients;
using AlgoRhythm.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Identity;

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
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add In-Memory Database for testing
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

            // Replace real EmailSender with mock implementation
            var emailSenderDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailSender));
            if (emailSenderDescriptor != null)
                services.Remove(emailSenderDescriptor);

            services.AddScoped<IEmailSender, MockEmailSender>();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });
    }
}

/// <summary>
/// Mock email sender for integration tests - does not send real emails.
/// </summary>
public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
    {
        // In tests, we don't send real emails - just log the attempt
        _logger.LogInformation("[MOCK EMAIL] To: {To}, Subject: {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
