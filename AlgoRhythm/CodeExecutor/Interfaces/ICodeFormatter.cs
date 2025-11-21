namespace CodeExecutor.Interfaces
{
    public interface ICodeFormatter
    {
        public string CodeTemplate { get; }
        public string Format(string code);
    }
}
