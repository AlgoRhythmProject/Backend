using AlgoRhythm.Data;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AlgoRhythm.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Cors
builder.AddFrontendCors();

// Database
builder.AddDbContext();

// ASP.NET Core Identity
builder.AddIdentity();

builder.ConfigureServices();
builder.ConfigureClients();
builder.ConfigureAuthentication();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT Authorization
builder.AddSwagger();

var app = builder.Build();

app.UseCors("AllowFrontend");

if (!app.Environment.IsEnvironment("Testing"))
{
    // Applying migrations
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying migrations...");
        await context.Database.MigrateAsync(); // ensures Roles, Users, etc. exist
        logger.LogInformation("Database ready!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying migrations.");
        throw;
    }

    if (app.Environment.IsDevelopment())
    {
        try
        {
            logger.LogInformation("Seeding the data...");

            await DbSeeder.SeedAsync(services);

            logger.LogInformation("Database seeded!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't seed the database");
        }
    }
}

// Seed default roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

    if (!await roleManager.RoleExistsAsync("User"))
    {
        await roleManager.CreateAsync(new Role
        {
            Name = "User",
            Description = "Default user role"
        });
    }

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new Role
        {
            Name = "Admin",
            Description = "Administrator with full access"
        });
    }
}

// Swagger UI (default path /swagger)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
await app.RunAsync();