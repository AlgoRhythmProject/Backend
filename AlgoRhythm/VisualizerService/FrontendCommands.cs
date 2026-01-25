namespace VisualizerService
{
    /// <summary>
    /// Class defining constants for events sent to frontend
    /// </summary>
    public static class FrontendCommands
    {
        // Graph
        public const string UpdateEdgeColor = nameof(UpdateEdgeColor);
        public const string UpdateEdgeLabel = nameof(UpdateEdgeLabel);
        public const string UpdateNodeColor = nameof(UpdateNodeColor);

        // Info
        public const string AddLog = nameof(AddLog);
        public const string ExecutionError = nameof(ExecutionError);
        public const string CompilationError = nameof(CompilationError);
        public const string ExecutionFinished = nameof(ExecutionFinished);
    }
}
