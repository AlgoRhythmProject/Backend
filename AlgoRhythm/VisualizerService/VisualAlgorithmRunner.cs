using AlgoRhythm.Shared.Helpers;
using Graph;
using Microsoft.AspNetCore.SignalR;
using System.Reflection;

namespace VisualizerService
{
    public class VisualAlgorithmRunner
    {
        private readonly IHubContext<VisualizerHub> _hubContext;
        private readonly CSharpCompiler _codeCompiler;

        public VisualAlgorithmRunner(IHubContext<VisualizerHub> context, CSharpCompiler compiler)
        {
            _hubContext = context;
            _codeCompiler = compiler;
        }

        public async Task ExecuteVisualAsync(
            string code,
            string sessionId,
            Node? startNode,
            Node? endNode,
            List<Node> nodes,
            List<Edge> edges,
            SessionState state)
        {
            CancellationToken ct = state.CTS.Token;

            await _hubContext.Clients.Group(sessionId).SendAsync("AddLog", "Compiling code...", ct);

            var result = _codeCompiler.Compile(code, "Solve");
            if (!result.Success)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
                await _hubContext.Clients.Group(sessionId).SendAsync("ExecutionError", $"Compilation failed: {errors}", ct);
                return;
            }

            await _hubContext.Clients.Group(sessionId).SendAsync("AddLog", "Compilation successful", ct);
            await _hubContext.Clients.Group(sessionId).SendAsync("AddLog", "Starting algorithm...", ct);

            try
            {
                if (result.AssemblyStream is null) {
                    await _hubContext.Clients.Group(sessionId).SendAsync("CompilationError", "Unknown error", ct);
                    return;
                }

                var assembly = Assembly.Load(result.AssemblyStream.ToArray());
                var type = assembly.GetType("Solution");

                if (type == null)
                {
                    await _hubContext.Clients.Group(sessionId).SendAsync("ExecutionError", "Class 'Solution' not found", ct);
                    return;
                }

                var method = type.GetMethod("Solve");
                if (method == null)
                {
                    await _hubContext.Clients.Group(sessionId).SendAsync("ExecutionError", "Method 'Solve' not found", ct);
                    return;
                }

                var instance = Activator.CreateInstance(type);

                var proxy = new SignalRGraphProxy(_hubContext, sessionId, startNode, endNode, nodes, edges, state);

                var parameters = method.GetParameters();

                if (method.ReturnType == typeof(Task))
                {
                    await (Task)method.Invoke(instance, parameters.Length > 0 ? [proxy] : null)!;
                }
                    
                await _hubContext.Clients.Group(sessionId).SendAsync("AddLog", "Algorithm completed", ct);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException)
            {
                await _hubContext.Clients.Group(sessionId).SendAsync("AddLog", "Algorithm stopped by user", ct);
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                await _hubContext.Clients.Group(sessionId).SendAsync("ExecutionError", $"Runtime Error: {errorMsg}", ct);
                await _hubContext.Clients.Group(sessionId).SendAsync("AddLog", $"{errorMsg}", ct);
            }
        }
    }
}
