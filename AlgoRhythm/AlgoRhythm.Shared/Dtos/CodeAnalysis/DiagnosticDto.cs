namespace AlgoRhythm.Shared.Dtos.CodeAnalysis
{
    public class DiagnosticDto
    {
        public string Message { get; set; } = "";
        public int Severity { get; set; }
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
        public string Id { get; set; } = "";
    }
}
