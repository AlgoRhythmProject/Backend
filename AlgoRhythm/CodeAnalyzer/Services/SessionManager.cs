using CodeAnalyzer.Interfaces;
using System.Collections.Concurrent;

namespace CodeAnalyzer.Services
{
    public sealed class SessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
        private readonly IWorkspaceFactory _workspaceFactory;

        public SessionManager(IWorkspaceFactory workspaceFactory)
        {
            _workspaceFactory = workspaceFactory;
        }

        public SessionState GetOrCreate(string connectionId)
        {
            return _sessions.GetOrAdd(connectionId, _ =>
            {
                var (workspace, projectId, documentId) = _workspaceFactory.Create();

                return new SessionState(workspace, projectId, documentId);
            });
        }

        public void Remove(string connectionId)
        {
            if (_sessions.TryRemove(connectionId, out var session))
            {
                session.Workspace.Dispose();
            }
        }
    }

}
