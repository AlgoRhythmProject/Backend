using Graph;
using Microsoft.AspNetCore.SignalR;

namespace VisualizerService
{
    public class SignalRGraphProxy : IGraph
    {
        private readonly IHubContext<VisualizerHub> _hubContext;
        private readonly string _sessionId;
        private readonly SessionState _state;

        private readonly List<Node> _nodes;
        private readonly List<Edge> _edges;

        public string StartNodeId { get; }
        public string? EndNodeId { get; }


        public SignalRGraphProxy(
            IHubContext<VisualizerHub> hubContext, 
            string sessionId,
            string startNodeId,
            string? endNodeId,
            List<Node> nodes,
            List<Edge> edges,
            SessionState state
        )
        {
            _hubContext = hubContext;
            _sessionId = sessionId;
            StartNodeId = startNodeId;
            EndNodeId = endNodeId;
            _nodes = nodes;
            _edges = edges;
            _state = state;
        }

        public async Task<List<Node>> GetNeighbors(string nodeId)
        {
            _state.CTS.Token.ThrowIfCancellationRequested();
            await _state.WaitIfPausedAsync();
            return _edges.Where(e => e.From == nodeId)
                            .Select(e => new Node { Id = e.From == nodeId ? e.To : e.From })
                            .ToList();
            
        }

        private async Task SendAsync(string method, params object[] args)
        {
            _state.CTS.Token.ThrowIfCancellationRequested();
            await _state.WaitIfPausedAsync();
            await _hubContext.Clients.Group(_sessionId).SendCoreAsync(method, args, _state.CTS.Token);
        }

        public async Task SetNodeColor(string nodeId, string color)
        {
            _state.CTS.Token.ThrowIfCancellationRequested();
            await _state.WaitIfPausedAsync();
            await _hubContext.Clients.Group(_sessionId).SendAsync("UpdateNodeColor", nodeId, color, _state.CTS.Token);
            await Task.Delay(50, _state.CTS.Token);
        }

        public async Task HighlightEdge(string fromId, string toId, string color)
        {
            _state.CTS.Token.ThrowIfCancellationRequested();
            await _state.WaitIfPausedAsync();
            await SendAsync("UpdateEdgeColor", fromId, toId, color);
            await Task.Delay(50, _state.CTS.Token);
        }

        public async Task SetEdgeLabel(string fromId, string toId, string label)
        {
            _state.CTS.Token.ThrowIfCancellationRequested();
            await _state.WaitIfPausedAsync();
            await SendAsync("UpdateEdgeLabel", fromId, toId, label);
        }

        public async Task Log(string message)
        {
            _state.CTS.Token.ThrowIfCancellationRequested();
            await _state.WaitIfPausedAsync();
            await SendAsync("AddLog", message);
        }

        public async Task Sleep(int ms)
        {
            _state.CTS.Token.ThrowIfCancellationRequested();
            await _state.WaitIfPausedAsync();
            await Task.Delay(ms, _state.CTS.Token);
        }
    }
}
