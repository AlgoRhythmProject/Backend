using AlgoRhythm.Clients;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.CodeExecutorTests
{
    public class CodeExecutorIntegrationTests : IClassFixture<CodeExecutorFixture>
    {
        private readonly CodeExecutorFixture _fixture;

        public CodeExecutorIntegrationTests(CodeExecutorFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Execute_Addition_Works_Through_Http()
        {
            CodeExecutorClient _client = _fixture.ServerFactory.Services.GetRequiredService<CodeExecutorClient>();
            // Arrange
            var request = new ExecuteCodeRequest
            {
                Code = @"using System;
                     public class Solution {
                         public int Solve(int a, int b) => a + b;
                     }",
                Args =
                [
                    new() { Name = "a", Value = "5" },
                    new() { Name = "b", Value = "7" }
                ],
                Timeout = TimeSpan.FromSeconds(1),
                ExpectedValue = "12",
                MaxPoints = 1
            };

            // Act
            var result = (await _client.ExecuteAsync([request])).First();

            // Assert
            Assert.True(result.Passed);
            Assert.Equal(request.ExpectedValue, result.ReturnedValue?.ToString());
            Assert.Equal(0, result.ExitCode);
        }
    }
}