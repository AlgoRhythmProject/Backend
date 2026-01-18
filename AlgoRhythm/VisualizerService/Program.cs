using AlgoRhythm.Shared.Helpers;
using VisualizerService;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<CSharpCompiler>();
builder.Services.AddScoped<CSharpCodeFormatter>();
builder.Services.AddTransient<VisualAlgorithmRunner>();

var app = builder.Build();

app.UseCors("AllowFrontend");
app.MapHub<VisualizerHub>("/visualizerhub");
app.Run();
