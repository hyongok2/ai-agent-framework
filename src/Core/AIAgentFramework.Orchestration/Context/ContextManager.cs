
using AIAgentFramework.Core.Common.Registry;
using AIAgentFramework.Core.Orchestration.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AIAgentFramework.Orchestration.Context;

public class ContextManager
{
    private readonly ConcurrentDictionary<string, IOrchestrationContext> _contexts = new();
    private readonly ILogger<ContextManager> _logger;
    private readonly IRegistry _registry;

    public ContextManager(ILogger<ContextManager> logger, IRegistry registry)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public IOrchestrationContext CreateContext(string userRequest)
    {
        var context = new OrchestrationContext(userRequest, _registry);
        _contexts[context.SessionId] = context;
        return context;
    }

    public IOrchestrationContext? GetContext(string sessionId)
    {
        _contexts.TryGetValue(sessionId, out var context);
        return context;
    }

    public void UpdateContext(IOrchestrationContext context)
    {
        if (context != null)
        {
            _contexts[context.SessionId] = context;
        }
    }

    public void RemoveContext(string sessionId)
    {
        _contexts.TryRemove(sessionId, out _);
    }

    public void CleanupExpiredContexts(TimeSpan expiration)
    {
        var expiredSessions = _contexts
            .Where(kvp => DateTime.UtcNow - kvp.Value.StartedAt > expiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in expiredSessions)
        {
            RemoveContext(sessionId);
        }
    }

    public int GetActiveContextCount() => _contexts.Count;
}