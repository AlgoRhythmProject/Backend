namespace CodeExecutor.DTO.Requests
{
    public class ExecuteCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public required string Language { get; set; }
    }
}
