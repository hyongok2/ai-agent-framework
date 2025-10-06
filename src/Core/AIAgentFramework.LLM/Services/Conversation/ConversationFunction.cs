using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services.Conversation;

/// <summary>
/// 대화 LLM Function - 일반 대화 및 질문 답변 (비구조화)
/// </summary>
public class ConversationFunction : LLMFunctionBase<ConversationInput, ConversationResult>
{
    public ConversationFunction(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
        : base(promptRegistry, llmProvider, options, logger)
    {
    }

    public override LLMRole Role => LLMRole.Conversationalist;

    public override string Description => "일반 대화 및 질문 답변 - 친절하고 자연스러운 응답";

    protected override string GetPromptName() => "conversation";

    protected override ConversationInput ExtractInput(ILLMContext context)
    {
        return new ConversationInput
        {
            UserMessage = context.UserInput,
            ConversationHistory = context.Parameters.TryGetValue("HISTORY", out var history) ? history?.ToString() : null,
            SystemContext = context.Parameters.TryGetValue("CONTEXT", out var ctx) ? ctx?.ToString() : null
        };
    }

    protected override IReadOnlyDictionary<string, object> PrepareVariables(ConversationInput input)
    {
        var variables = new Dictionary<string, object>
        {
            ["USER_MESSAGE"] = input.UserMessage
        };

        if (!string.IsNullOrWhiteSpace(input.ConversationHistory))
        {
            variables["CONVERSATION_HISTORY"] = input.ConversationHistory;
        }

        if (!string.IsNullOrWhiteSpace(input.SystemContext))
        {
            variables["SYSTEM_CONTEXT"] = input.SystemContext;
        }

        return variables;
    }

    protected override ConversationResult ParseResponse(string rawResponse)
    {
        // JSON 파싱 시도
        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(rawResponse);
            var root = jsonDoc.RootElement;

            return new ConversationResult
            {
                Response = root.TryGetProperty("Response", out var resp) ? resp.GetString() ?? rawResponse : rawResponse,
                Tone = root.TryGetProperty("Tone", out var tone) ? tone.GetString() : null,
                Metadata = root.TryGetProperty("Metadata", out var meta)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(meta.GetRawText())
                    : null
            };
        }
        catch
        {
            // JSON 파싱 실패 시 전체를 Response로 사용 (비구조화 응답 허용)
            return new ConversationResult
            {
                Response = rawResponse.Trim(),
                Tone = "friendly"
            };
        }
    }

    protected override ILLMResult CreateResult(string rawResponse, ConversationResult parsedData)
    {
        return new LLMResult
        {
            IsSuccess = true,
            RawResponse = rawResponse,
            ParsedData = parsedData
        };
    }
}
