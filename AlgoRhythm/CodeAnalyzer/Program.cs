using CodeAnalyzer;
using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Services;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:5173") 
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});
builder.Services.AddSignalR();

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

app.UseCors();
app.MapHub<RoslynHub>("/roslynhub");

app.Run();