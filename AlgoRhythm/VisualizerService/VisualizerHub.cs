using Graph;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace VisualizerService
{
    public class VisualizerHub : Hub
    {
        private readonly VisualAlgorithmRunner _runner;

        private static readonly ConcurrentDictionary<string, SessionState> _sessions = new();
        public VisualizerHub(VisualAlgorithmRunner runner)
        {
            _runner = runner;
        }

        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        public async Task StartAlgorithm(string sessionId, string code, List<Node> nodes, List<Edge> edges, Node? startNode, Node? endNode)
        {
            StopAlgorithm(sessionId);
            await JoinSession(sessionId);

            var cts = new CancellationTokenSource();
            var newState = new SessionState(cts);

            _sessions[sessionId] = newState;

            try
            {
                await _runner.ExecuteVisualAsync(code, sessionId, startNode, endNode, nodes, edges, newState);

                await Clients.Group(sessionId).SendAsync("ExecutionFinished");
            }
            catch (OperationCanceledException) { /* Ignore - operation stopped manually */ }
            catch (Exception ex)
            {
                await Clients.Group(sessionId).SendAsync("ExecutionError", ex.Message);
            }
            finally
            {
                _sessions.TryRemove(sessionId, out _);
            }
        }

        public void StopAlgorithm(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.PauseTcs.TrySetResult(false);
                session.CTS.Cancel();
            }
        }

        public async Task PauseAlgorithm(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var state))
            {
                state.IsPaused = true;
                state.PauseTcs = new TaskCompletionSource<bool>();
                await Clients.Group(sessionId).SendAsync("AddLog", "Algorithm paused.");
            }
        }

        public async Task ResumeAlgorithm(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var state) && state.IsPaused)
            {
                state.IsPaused = false;
                state.PauseTcs.TrySetResult(true);
                await Clients.Group(sessionId).SendAsync("AddLog", "Algorithm resumed.");
            }
        }

    }
}
