namespace CodeExecutor.DTO.Requests
{
    public class ExecuteCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string ReturnType { get; set; } = "void";
        public Dictionary<(string type, string id), object?> Args { get; set; } = new();
    }
}
