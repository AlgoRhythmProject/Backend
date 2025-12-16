namespace AlgoRhythm.Shared.Dtos.CodeAnalysis
{
    public class CompletionItemDto
    {
        public string Label { get; set; } = "";
        public int Kind { get; set; }
        public string InsertText { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Documentation { get; set; } = "";
        public string SortText { get; set; } = "";
    }
}
