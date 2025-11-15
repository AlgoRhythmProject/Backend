namespace CodeExecutor.DTO
{
    public class ExecutionResult
    {
        public string Stdout { get; set; } = string.Empty;
        public string Stderr { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public long ExitCode { get; set; }
        public bool Success { get; set; }
    }
}
