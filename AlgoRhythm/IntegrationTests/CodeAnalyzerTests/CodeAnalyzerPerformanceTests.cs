using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace IntegrationTests.CodeAnalyzerTests
{
    public class RoslynHubIntegrationTests : IClassFixture<WebApplicationFactory<CodeAnalyzer.Program>>
    {
        private readonly WebApplicationFactory<CodeAnalyzer.Program> _factory;
        private const string Code = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;

            namespace TestApp {
                public class DataProcessor {
                    public void Process() {
                        var items = new List<string> { ""A"", ""B"", ""C"" };
                        var count = items.Where(x => x.Length > 0).Count();
                        Console.WriteLine(count);
                    }
                }
            }";

        public RoslynHubIntegrationTests(WebApplicationFactory<CodeAnalyzer.Program> factory)
        {
            Environment.SetEnvironmentVariable("FRONTEND_URL", "http://localhost:80");
            _factory = factory;
        }

        [Trait("Category", "Performance")]
        [Fact(Skip = "Performance test - run manually")]
        public async Task FullCodeAnalysis_StressTest_100ConcurrentClients()
        {
            int clientCount = 50;
            var tasks = new List<Task>();
            var httpClient = _factory.CreateClient();
            var responseTimes = new ConcurrentBag<long>();

            for (int i = 0; i < clientCount; i++)
            {
                await Task.Delay(30);
                tasks.Add(Task.Run(async () =>
                {
                    var hubConnection = new HubConnectionBuilder()
                        .WithUrl($"{httpClient.BaseAddress}roslynhub", options =>
                        {
                            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                        })
                        .Build();

                    try
                    {
                        await hubConnection.StartAsync();

                        // 1. AnalyzeCode 
                        var swAnalyze = Stopwatch.StartNew();
                        await hubConnection.InvokeAsync<object[]>("AnalyzeCode", Code);
                        swAnalyze.Stop();
                        responseTimes.Add(swAnalyze.ElapsedMilliseconds);

                        // 2. QuickInfo 
                        var swQuick = Stopwatch.StartNew();
                        await hubConnection.InvokeAsync<object>("GetQuickInfo", Code, 8, 32);
                        swQuick.Stop();
                        responseTimes.Add(swQuick.ElapsedMilliseconds);

                        // 3. GetCompletions
                        var swComp = Stopwatch.StartNew();
                        await hubConnection.InvokeAsync<object[]>("GetCompletions", Code, 8, 32);
                        swComp.Stop();
                        responseTimes.Add(swComp.ElapsedMilliseconds);
                    }
                    finally
                    {
                        await hubConnection.DisposeAsync();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            var avg = responseTimes.Average();
            var max = responseTimes.Max();


            Assert.True(avg < 1000, $"Average time {avg}ms > 1000ms");
            Assert.True(max < 2000, $"Max time {max}ms > 2s");
        }
    }
}