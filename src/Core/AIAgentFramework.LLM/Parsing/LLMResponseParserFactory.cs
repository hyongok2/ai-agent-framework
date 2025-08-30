using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.LLM.Parsing.Models;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.LLM.Parsing;

/// <summary>
/// LLM 응답 파서 팩토리
/// </summary>
public class LLMResponseParserFactory
{
    private readonly ILLMResponseParser _parser;
    private readonly ILogger<LLMResponseParserFactory> _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="parser">응답 파서</param>
    /// <param name="logger">로거</param>
    public LLMResponseParserFactory(ILLMResponseParser parser, ILogger<LLMResponseParserFactory> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 역할별 응답 파싱
    /// </summary>
    /// <param name="role">LLM 기능 역할</param>
    /// <param name="response">응답</param>
    /// <returns>파싱된 응답</returns>
    public object? ParseByRole(string role, string response)
    {
        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            return role.ToLowerInvariant() switch
            {
                "planner" => _parser.ParseJson<PlannerResponse>(response),
                "interpreter" => _parser.ParseJson<InterpreterResponse>(response),
                "summarizer" => _parser.ParseJson<SummarizerResponse>(response),
                "generator" => _parser.ParseJson<GeneratorResponse>(response),
                "evaluator" => _parser.ParseJson<EvaluatorResponse>(response),
                "rewriter" => _parser.ParseJson<RewriterResponse>(response),
                "explainer" => _parser.ParseJson<ExplainerResponse>(response),
                "reasoner" => _parser.ParseJson<ReasonerResponse>(response),
                "converter" => _parser.ParseJson<ConverterResponse>(response),
                "visualizer" => _parser.ParseJson<VisualizerResponse>(response),
                "tool_parameter_setter" => _parser.ParseJson<ToolParameterResponse>(response),
                "dialogue_manager" => _parser.ParseJson<DialogueResponse>(response),
                "knowledge_retriever" => _parser.ParseJson<KnowledgeResponse>(response),
                "meta_manager" => _parser.ParseJson<MetaResponse>(response),
                _ => ParseGenericResponse(response)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse response for role: {Role}", role);
            return null;
        }
    }

    /// <summary>
    /// 응답 타입별 파싱
    /// </summary>
    /// <param name="responseType">응답 타입</param>
    /// <param name="response">응답</param>
    /// <returns>파싱된 응답</returns>
    public object? ParseByType(LLMResponseType responseType, string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            return responseType switch
            {
                LLMResponseType.StructuredJson => ParseGenericResponse(response),
                LLMResponseType.UserQuestion => _parser.ParseJson<UserQuestionResponse>(response),
                LLMResponseType.Error => _parser.ParseJson<ErrorResponse>(response),
                LLMResponseType.SpecialFunction => _parser.ParseJson<SpecialFunctionResponse>(response),
                LLMResponseType.PlainText => new { content = response, type = "plain_text" },
                LLMResponseType.Completion => new { content = response, type = "completion", is_completed = true },
                _ => ParseGenericResponse(response)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse response for type: {Type}", responseType);
            return null;
        }
    }

    /// <summary>
    /// 일반적인 응답 파싱
    /// </summary>
    /// <param name="response">응답</param>
    /// <returns>파싱된 응답</returns>
    private object? ParseGenericResponse(string response)
    {
        // 먼저 구조화된 응답인지 확인
        if (_parser.ValidateStructuredResponse(response, "success", "data") ||
            _parser.ValidateStructuredResponse(response, "is_completed"))
        {
            return _parser.ParseJson<BaseLLMResponse>(response);
        }

        // 일반 딕셔너리로 파싱 시도
        return _parser.ParseJson<Dictionary<string, object>>(response);
    }

    /// <summary>
    /// 응답 검증
    /// </summary>
    /// <param name="role">역할</param>
    /// <param name="response">응답</param>
    /// <returns>검증 결과</returns>
    public bool ValidateResponse(string role, string response)
    {
        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(response))
            return false;

        try
        {
            var requiredFields = GetRequiredFieldsByRole(role);
            return _parser.ValidateStructuredResponse(response, requiredFields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate response for role: {Role}", role);
            return false;
        }
    }

    /// <summary>
    /// 역할별 필수 필드 반환
    /// </summary>
    /// <param name="role">역할</param>
    /// <returns>필수 필드 배열</returns>
    private string[] GetRequiredFieldsByRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "planner" => new[] { "actions", "is_completed" },
            "interpreter" => new[] { "interpretation", "is_completed" },
            "summarizer" => new[] { "summary", "is_completed" },
            "generator" => new[] { "generated_content", "is_completed" },
            "evaluator" => new[] { "evaluation", "is_completed" },
            "rewriter" => new[] { "rewritten_content", "is_completed" },
            "explainer" => new[] { "explanation", "is_completed" },
            "reasoner" => new[] { "reasoning", "is_completed" },
            "converter" => new[] { "converted_content", "is_completed" },
            "visualizer" => new[] { "visualization", "is_completed" },
            "tool_parameter_setter" => new[] { "parameters", "is_completed" },
            "dialogue_manager" => new[] { "response", "is_completed" },
            "knowledge_retriever" => new[] { "knowledge", "is_completed" },
            "meta_manager" => new[] { "meta_action", "is_completed" },
            _ => new[] { "is_completed" }
        };
    }
}

// 추가 응답 모델들 (간단한 구조)
public class EvaluatorResponse : BaseLLMResponse
{
    public string Evaluation { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<string> Criteria { get; set; } = new();
}

public class RewriterResponse : BaseLLMResponse
{
    public string RewrittenContent { get; set; } = string.Empty;
    public List<string> Changes { get; set; } = new();
}

public class ExplainerResponse : BaseLLMResponse
{
    public string Explanation { get; set; } = string.Empty;
    public List<string> KeyConcepts { get; set; } = new();
}

public class ReasonerResponse : BaseLLMResponse
{
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
}

public class ConverterResponse : BaseLLMResponse
{
    public string ConvertedContent { get; set; } = string.Empty;
    public string SourceFormat { get; set; } = string.Empty;
    public string TargetFormat { get; set; } = string.Empty;
}

public class VisualizerResponse : BaseLLMResponse
{
    public string Visualization { get; set; } = string.Empty;
    public string VisualizationType { get; set; } = string.Empty;
}

public class ToolParameterResponse : BaseLLMResponse
{
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string ToolName { get; set; } = string.Empty;
}

public class DialogueResponse : BaseLLMResponse
{
    public string Response { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public class KnowledgeResponse : BaseLLMResponse
{
    public string Knowledge { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
}

public class MetaResponse : BaseLLMResponse
{
    public string MetaAction { get; set; } = string.Empty;
    public Dictionary<string, object> ActionParameters { get; set; } = new();
}