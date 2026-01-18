namespace VisualizerService
{
    public class SessionState
    {
        public CancellationTokenSource CTS { get; set; }
        public TaskCompletionSource<bool> PauseTcs { get; set; } = new();
        public bool IsPaused { get; set; } = false;

        public SessionState(CancellationTokenSource cts) => CTS = cts;

        public async Task WaitIfPausedAsync()
        {
            if (IsPaused) await PauseTcs.Task;
        }
    }
}
