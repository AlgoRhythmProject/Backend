//using AlgoRhythm;
//using AlgoRhythm.Clients;
//using AlgoRhythm.Data;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Mvc.Testing;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using System.Runtime.Intrinsics.X86;

//namespace IntegrationTests.CodeExecutorTests
//{
//    public class CodeExecutorFixture : IDisposable
//    {
//        internal WebApplicationFactory<Program> ServerFactory { get; }
//        private readonly WebApplicationFactory<CodeExecutor.Program> _serviceFactory;

//        public CodeExecutorFixture()
//        {
//            // // Test JWT_KEY
//            // Environment.SetEnvironmentVariable("JWT_KEY", "TestSuperSecretKey123!");

//            // CodeExecutor service
//            _serviceFactory = new WebApplicationFactory<CodeExecutor.Program>();
//            var codeExecutorHttpClient = _serviceFactory.CreateClient();
//            var codeExecutorBaseUrl = codeExecutorHttpClient.BaseAddress!.ToString();

//            // Start the main server and configure it to use the test CodeExecutor service
//            ServerFactory = new WebApplicationFactory<Program>()
//                .WithWebHostBuilder(builder =>
//                {
//                    // builder.UseEnvironment("Test"); // important

//                    builder.ConfigureServices(services =>
//                    {
//                        //// Remove production DbContext
//                        //var dbDescriptor = services.SingleOrDefault(
//                        //    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
//                        //);
//                        //if (dbDescriptor != null)
//                        //    services.Remove(dbDescriptor);

//                        //// Use InMemory database
//                        //services.AddDbContext<ApplicationDbContext>(options =>
//                        //    options.UseInMemoryDatabase("TestDb"));

//                        //// Optional: seed Identity users/roles if needed
//                        //var sp = services.BuildServiceProvider();
//                        //using var scope = sp.CreateScope();
//                        //var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//                        //context.Database.EnsureCreated();

//                        // Remove and re-register CodeExecutorClient
//                        var descriptor = services.SingleOrDefault(d =>
//                            d.ServiceType == typeof(CodeExecutorClient));
//                        if (descriptor != null)
//                            services.Remove(descriptor);

//                        services.AddSingleton(sp =>
//                        {
//                            var http = _serviceFactory.CreateClient();
//                            return new CodeExecutorClient(http);
//                        });
//                    });
//                });

//        }

//        public void Dispose()
//        {
//            _serviceFactory.Dispose();
//            ServerFactory.Dispose();
//        }
//    }
//}