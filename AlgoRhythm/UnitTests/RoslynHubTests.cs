using Microsoft.AspNetCore.SignalR;
using Moq;
using CodeAnalyzer;
using CodeAnalyzer.Interfaces;
using AlgoRhythm.Shared.Dtos.CodeAnalysis;

namespace UnitTests
{
    public class RoslynHubTests
    {
        [Fact]
        public async Task AnalyzeCode_ConcurrentCalls_ReturnsDiagnostics()
        {
            var diagnosticServiceMock = new Mock<IDiagnosticService>();
            diagnosticServiceMock
                .Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync([]); 

            var completionServiceMock = new Mock<ICompletionService>();
            var quickInfoServiceMock = new Mock<IQuickInfoService>();
            var sessionManagerMock = new Mock<ISessionManager>();

            var hubContextMock = new Mock<HubCallerContext>();
            hubContextMock.Setup(c => c.ConnectionId).Returns(Guid.NewGuid().ToString());

            var hub = new RoslynHub(
                completionServiceMock.Object,
                quickInfoServiceMock.Object,
                diagnosticServiceMock.Object,
                sessionManagerMock.Object
            )
            {
                Context = hubContextMock.Object
            };

            // Simulating 100 concurrent invokes
            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var diagnostics = await hub.AnalyzeCode("class C { void M() { } }");
                    Assert.NotNull(diagnostics); 
                }));
            }

            await Task.WhenAll(tasks);

            diagnosticServiceMock.Verify(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(100));
        }
    }
}
