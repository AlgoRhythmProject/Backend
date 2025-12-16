namespace CodeAnalyzer.Interfaces
{
    public interface ISessionManager
    {
        SessionState GetOrCreate(string connectionId);
        void Remove(string connectionId);
    }
}
