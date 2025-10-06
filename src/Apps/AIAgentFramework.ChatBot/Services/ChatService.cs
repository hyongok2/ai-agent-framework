using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Core.Services.ChatHistory;
using AIAgentFramework.Execution.Services;

namespace AIAgentFramework.ChatBot.Services;

/// <summary>
/// 채팅 서비스 - Orchestrator와 ChatHistory를 통합
/// </summary>
public class ChatService
{
    private readonly IOrchestrator _orchestrator;
    private readonly IChatHistoryStore _historyStore;
    private readonly StreamingRenderer _renderer;
    private string? _currentSessionId;

    public ChatService(
        IOrchestrator orchestrator,
        IChatHistoryStore historyStore,
        StreamingRenderer renderer)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public async Task<string> StartNewSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await _historyStore.CreateSessionAsync(
            userId: "console-user",
            title: $"Chat {DateTime.Now:yyyy-MM-dd HH:mm}",
            cancellationToken: cancellationToken);

        _currentSessionId = session.SessionId;
        _renderer.RenderSystemMessage($"New session started: {session.SessionId}");
        return session.SessionId;
    }

    public async Task ProcessUserInputAsync(
        string userInput,
        IAgentContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            await StartNewSessionAsync(cancellationToken);
        }

        // 사용자 메시지 저장
        await _historyStore.AddMessageAsync(
            _currentSessionId!,
            MessageRole.User,
            userInput,
            cancellationToken: cancellationToken);

        _renderer.RenderUserMessage(userInput);

        // 히스토리 컨텍스트 추가
        var messages = await _historyStore.GetMessagesAsync(_currentSessionId!, cancellationToken: cancellationToken);
        var historyContext = ChatHistoryFormatter.FormatAsContext(messages, maxMessages: 10);

        // Context에 히스토리 추가
        context.Set("HISTORY", historyContext["CONVERSATION_HISTORY"]);
        context.Set("LAST_USER_MESSAGE", historyContext["LAST_USER_MESSAGE"]);
        context.Set("MESSAGE_COUNT", historyContext["MESSAGE_COUNT"]);

        // Assistant 응답 스트리밍
        _renderer.RenderAssistantStart();
        var assistantResponse = "";

        await foreach (var chunk in _orchestrator.ExecuteStreamAsync(userInput, context, cancellationToken))
        {
            _renderer.RenderChunk(chunk);

            if (chunk.Type == StreamChunkType.Text && !string.IsNullOrEmpty(chunk.Content))
            {
                assistantResponse += chunk.Content;
            }
        }

        // Assistant 메시지 저장
        if (!string.IsNullOrEmpty(assistantResponse))
        {
            await _historyStore.AddMessageAsync(
                _currentSessionId!,
                MessageRole.Assistant,
                assistantResponse,
                cancellationToken: cancellationToken);
        }
    }

    public async Task<List<ChatMessage>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            return new List<ChatMessage>();
        }

        return await _historyStore.GetMessagesAsync(_currentSessionId, cancellationToken: cancellationToken);
    }
}
