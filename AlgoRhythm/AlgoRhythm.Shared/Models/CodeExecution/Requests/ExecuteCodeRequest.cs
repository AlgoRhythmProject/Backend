namespace AlgoRhythm.Shared.Models.CodeExecution.Requests
{
    public class ExecuteCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public List<FunctionParameter> Args { get; set; } = new();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public string ExecutionClass { get; set; } = "Solution";
        public string ExecutionMethod { get; set; } = "Solve";
    }
}
