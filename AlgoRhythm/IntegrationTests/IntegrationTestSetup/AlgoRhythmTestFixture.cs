using AlgoRhythm.Data;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.IntegrationTestSetup
{
    public class AlgoRhythmTestFixture : IAsyncLifetime
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
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            
            await dbContext.Database.EnsureCreatedAsync();

            // SEED ROLES
            await SeedRolesAsync(roleManager);
        }

        private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
        {
            string[] roleNames = { "User", "Admin" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Role
                    {
                        Name = roleName,
                        Description = roleName == "Admin" ? "Administrator with full access" : "Default user role"
                    });
                }
            }
        }

        public async Task DisposeAsync()
        {
            await ServerFactory.DisposeAsync();
            await ExecutorFactory.DisposeAsync();
        }
    }
}