namespace CodeExecutor.Helpers
{
    /// <summary>
    /// Captures console output (<see cref="Console.Out"/> and <see cref="Console.Error"/>)
    /// and redirects it to in-memory <see cref="StringWriter"/> instances.
    /// Restores the original console output when disposed.
    /// </summary>
    public class ConsoleOrchestrator : IDisposable
    {
        private readonly TextWriter originalOut = Console.Out;
        private readonly TextWriter originalError = Console.Error;
        public StringWriter StdOut { get; } = new();
        public StringWriter StdErr { get; } = new();

        public ConsoleOrchestrator()
        {
            Console.SetOut(StdOut);
            Console.SetError(StdErr);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
    }
}
