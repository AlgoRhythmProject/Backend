using CodeExecutor.Helpers;
using CodeExecutor.Services;
using Docker.DotNet;

namespace CodeExecutor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();            
            builder.Services.AddScoped<CSharpExecuteService>();
            builder.Services.AddScoped<CSharpCodeFormatter>();
            builder.Services.AddScoped<CSharpCompiler>();
            builder.Services.AddSingleton(_ =>
            {
                return new DockerClientConfiguration(
                    new Uri(Environment.OSVersion.Platform == PlatformID.Win32NT
                        ? "npipe://./pipe/docker_engine"    // Windows
                        : "unix:///var/run/docker.sock")    // Linux/macOS
                ).CreateClient();
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
