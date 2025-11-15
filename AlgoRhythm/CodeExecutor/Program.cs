
using CodeExecutor.Config;
using CodeExecutor.Services;
using Docker.DotNet;

namespace CodeExecutor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add configuration
            builder.Configuration.AddJsonFile("./Config/codeexecutionconfig.json", optional: false, reloadOnChange: true);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.Configure<CSharpCodeExecutionConfig>(
                builder.Configuration.GetSection(nameof(CSharpCodeExecutionConfig)));
            builder.Services.AddScoped<CSharpExecutionService>();
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

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
