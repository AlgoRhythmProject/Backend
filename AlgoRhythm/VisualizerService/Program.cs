using AlgoRhythm.Shared.Helpers;
using VisualizerService;

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
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<CSharpCompiler>();
builder.Services.AddScoped<CSharpCodeFormatter>();
builder.Services.AddTransient<VisualAlgorithmRunner>();

var app = builder.Build();


app.UseCors();
app.MapHub<VisualizerHub>("/visualizerhub");
app.Run();
