namespace AlgoRhythm.Shared.Models.CodeExecution.Requests
{
    public class ExecuteCodeRequest
    {
        public Guid TestCaseId { get; set; }
        public string Code { get; set; } = string.Empty;
        public List<FunctionParameter> Args { get; set; } = [];
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public string ExecutionClass { get; set; } = "Solution";
        public string ExecutionMethod { get; set; } = "Solve";
        public string ExpectedValue { get; set; } = string.Empty;
        public int MaxPoints { get; set; } = 10;
    }
}