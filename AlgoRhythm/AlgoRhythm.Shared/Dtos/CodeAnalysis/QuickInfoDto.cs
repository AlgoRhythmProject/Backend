namespace AlgoRhythm.Shared.Dtos.CodeAnalysis
{
    public class QuickInfoDto
    {
        public string Description { get; set; } = "";
        public string Documentation { get; set; } = "";
        public int SpanStart { get; set; }
        public int SpanLength { get; set; }
    }

}
