namespace AlgoRhythm.Shared.Models.CodeExecution.Responses
{
    public class ExecuteCodeResponse
    {
        public string Stdout { get; set; } = string.Empty;
        public string Stderr { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public long ExitCode { get; set; }
        public bool Success { get; set; }
        public object? ReturnedValue { get; set; } = string.Empty;
    }
}
