namespace Graph
{
    public interface IGraph
    {
        string StartNodeId { get; }
        string? EndNodeId { get; }
        Task SetNodeColor(string nodeId, string color);
        Task HighlightEdge(string fromId, string toId, string color);
        Task SetEdgeLabel(string fromId, string toId, string label);
        Task Log(string message);
        Task Sleep(int ms);
        Task<List<Node>> GetNeighbors(string nodeId);
    }
}
