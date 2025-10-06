using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Services.ChatHistory;

/// <summary>
/// 채팅 히스토리를 LLM 입력 형식으로 변환하는 헬퍼
/// </summary>
public static class ChatHistoryFormatter
{
    /// <summary>
    /// 채팅 히스토리를 텍스트 형식으로 변환
    /// </summary>
    public static string FormatAsText(List<ChatMessage> messages, int? maxMessages = null)
    {
        var messagesToFormat = maxMessages.HasValue
            ? messages.TakeLast(maxMessages.Value).ToList()
            : messages;

        var lines = new List<string>();

        foreach (var message in messagesToFormat)
        {
            var roleLabel = message.Role switch
            {
                MessageRole.User => "User",
                MessageRole.Assistant => "Assistant",
                MessageRole.System => "System",
                _ => "Unknown"
            };

            lines.Add($"{roleLabel}: {message.Content}");
        }

        return string.Join("\n\n", lines);
    }

    /// <summary>
    /// 채팅 히스토리를 Markdown 형식으로 변환
    /// </summary>
    public static string FormatAsMarkdown(List<ChatMessage> messages, int? maxMessages = null)
    {
        var messagesToFormat = maxMessages.HasValue
            ? messages.TakeLast(maxMessages.Value).ToList()
            : messages;

        var lines = new List<string>();

        foreach (var message in messagesToFormat)
        {
            var roleLabel = message.Role switch
            {
                MessageRole.User => "👤 User",
                MessageRole.Assistant => "🤖 Assistant",
                MessageRole.System => "⚙️ System",
                _ => "❓ Unknown"
            };

            lines.Add($"### {roleLabel}");
            lines.Add(message.Content);
            lines.Add(""); // 빈 줄
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 채팅 히스토리를 LLM Context 변수로 변환
    /// </summary>
    public static Dictionary<string, object> FormatAsContext(List<ChatMessage> messages, int? maxMessages = null)
    {
        var messagesToFormat = maxMessages.HasValue
            ? messages.TakeLast(maxMessages.Value).ToList()
            : messages;

        return new Dictionary<string, object>
        {
            ["CONVERSATION_HISTORY"] = FormatAsText(messagesToFormat),
            ["MESSAGE_COUNT"] = messagesToFormat.Count,
            ["LAST_USER_MESSAGE"] = messagesToFormat.LastOrDefault(m => m.Role == MessageRole.User)?.Content ?? "",
            ["LAST_ASSISTANT_MESSAGE"] = messagesToFormat.LastOrDefault(m => m.Role == MessageRole.Assistant)?.Content ?? ""
        };
    }

    /// <summary>
    /// OpenAI/Ollama 스타일 메시지 배열로 변환
    /// </summary>
    public static List<Dictionary<string, string>> FormatAsMessages(List<ChatMessage> messages, int? maxMessages = null)
    {
        var messagesToFormat = maxMessages.HasValue
            ? messages.TakeLast(maxMessages.Value).ToList()
            : messages;

        return messagesToFormat.Select(m => new Dictionary<string, string>
        {
            ["role"] = m.Role.ToString().ToLower(),
            ["content"] = m.Content
        }).ToList();
    }

    /// <summary>
    /// 토큰 수 기반으로 최근 메시지만 가져오기
    /// </summary>
    public static List<ChatMessage> GetMessagesWithinTokenLimit(List<ChatMessage> messages, int maxTokens)
    {
        var result = new List<ChatMessage>();
        var currentTokens = 0;

        // 최신 메시지부터 역순으로 추가
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            var message = messages[i];
            var messageTokens = message.TokenCount ?? EstimateTokenCount(message.Content);

            if (currentTokens + messageTokens > maxTokens)
            {
                break;
            }

            result.Insert(0, message);
            currentTokens += messageTokens;
        }

        return result;
    }

    /// <summary>
    /// 토큰 수 추정 (간단한 휴리스틱)
    /// </summary>
    private static int EstimateTokenCount(string text)
    {
        // 대략적인 추정: 영어는 4자당 1토큰, 한글은 2자당 1토큰
        var length = text.Length;
        return (int)Math.Ceiling(length / 3.0);
    }
}
