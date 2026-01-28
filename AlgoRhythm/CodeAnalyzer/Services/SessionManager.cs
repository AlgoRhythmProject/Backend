using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Models;
using System.Collections.Concurrent;

namespace CodeAnalyzer.Services
{
    /// <summary>
    /// Manages user coding sessions by maintaining isolated Roslyn workspaces.
    /// Ensures that each connected user has a dedicated environment for code analysis 
    /// and handles the lifecycle of these environments to prevent memory leaks.
    /// </summary>
    public sealed class SessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
        private readonly IWorkspaceFactory _workspaceFactory;

        public SessionManager(IWorkspaceFactory workspaceFactory)
        {
            _workspaceFactory = workspaceFactory;
        }

        /// <summary>
        /// Retrieves an existing session for the given connection ID or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="connectionId">The unique identifier for the user's connection (SignalR ConnectionId).</param>
        /// <returns>A <see cref="SessionState"/> containing the associated workspace, project, and document IDs.</returns>
        public SessionState GetOrCreate(string connectionId)
        {
            return _sessions.GetOrAdd(connectionId, _ =>
            {
                var (workspace, projectId, documentId) = _workspaceFactory.Create();

                return new SessionState(workspace, projectId, documentId);
            });
        }

        /// <summary>
        /// Removes a session and disposes of its associated resources.
        /// This should be called when a user disconnects to free up server memory.
        /// </summary>
        /// <param name="connectionId">The identifier of the session to terminate.</param>
        public void Remove(string connectionId)
        {
            if (_sessions.TryRemove(connectionId, out var session))
            {
                session.Workspace.Dispose();
            }
        }
    }

}
