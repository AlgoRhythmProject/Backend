using CodeAnalyzer;
using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Services;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace CodeAnalyzer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSignalR();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .WithOrigins(Environment.GetEnvironmentVariable("FRONTEND_URL")!)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.Services.AddSingleton<IReferenceProvider, RuntimeReferenceProvider>();
        builder.Services.AddSingleton<HostServices>(_ =>
            MefHostServices.Create(MefHostServices.DefaultAssemblies));

        builder.Services.AddSingleton<IWorkspaceFactory, WorkspaceFactory>();
        builder.Services.AddSingleton<ISessionManager, SessionManager>();

        builder.Services.AddSingleton<IDocumentService, DocumentService>();
        builder.Services.AddSingleton<ICompletionService, CompletionService>();
        builder.Services.AddSingleton<IQuickInfoService, QuickInfoRoslynService>();
        builder.Services.AddSingleton<IDiagnosticService, DiagnosticService>();

        var app = builder.Build();

        app.UseCors("AllowFrontend");
        app.MapHub<RoslynHub>("/roslynhub");

        app.Run();
    }
}