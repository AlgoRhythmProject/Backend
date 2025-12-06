using AlgoRhythm.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.CodeExecutorTests
{
    public class CodeExecutorFixture : IAsyncLifetime
    {
        internal WebApplicationFactory<CodeExecutor.Program> ExecutorFactory { get; private set; } = null!;
        internal AlgoRhythmWebApplicationFactory ServerFactory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            ExecutorFactory = new WebApplicationFactory<CodeExecutor.Program>();
            ServerFactory = new AlgoRhythmWebApplicationFactory(ExecutorFactory);

            var services = ServerFactory.Services;
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await dbContext.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await ServerFactory.DisposeAsync();
            await ExecutorFactory.DisposeAsync();
        }
    }
}