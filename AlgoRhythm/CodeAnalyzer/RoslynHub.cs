using AlgoRhythm.Shared.Dtos.CodeAnalysis;
using CodeAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CodeAnalyzer;

/// <summary>
/// SignalR hub providing Roslyn-based code analysis services such as
/// diagnostics, code completion and QuickInfo for C# source code.
/// </summary>
/// <remarks>
/// Each SignalR connection represents an isolated Roslyn session
/// with its own workspace and compilation state.
/// </remarks>
public class RoslynHub : Hub
{
    private readonly ICompletionService _completionService;
    private readonly IQuickInfoService _quickInfoService;
    private readonly IDiagnosticService _diagnosticService;
    private readonly ISessionManager _sessionManager;

    public RoslynHub(ICompletionService completionService, 
        IQuickInfoService quickInfoService,
        IDiagnosticService diagnosticService,
        ISessionManager sessionManager)
    {
        _completionService = completionService; 
        _quickInfoService = quickInfoService;
        _diagnosticService = diagnosticService;
        _sessionManager = sessionManager;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _sessionManager.Remove(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }


    public async Task<DiagnosticDto[]> AnalyzeCode(string code)
    {
        return await _diagnosticService.AnalyzeAsync(code, Context.ConnectionId);   
    }


    public async Task<CompletionItemDto[]> GetCompletions(string code, int line, int column)
    {
        return await _completionService.GetCompletionsAsync(code, line, column, Context.ConnectionId);
    }

    public async Task<QuickInfoDto?> GetQuickInfo(string code, int line, int column)
    {
        return await _quickInfoService.GetQuickInfoAsync(code, line, column, Context.ConnectionId);
    }
}

