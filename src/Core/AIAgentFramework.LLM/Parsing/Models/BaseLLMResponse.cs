using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Parsing.Models;

/// <summary>
/// 기본 LLM 응답 모델
/// </summary>
public abstract class BaseLLMResponse
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    /// <summary>
    /// 데이터
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// 다음 액션들
    /// </summary>
    [JsonPropertyName("next_actions")]
    public List<string> NextActions { get; set; } = new();

    /// <summary>
    /// 완료 여부
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 메타데이터
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 사용자 질문 응답
/// </summary>
public class UserQuestionResponse : BaseLLMResponse
{
    /// <summary>
    /// 질문 내용
    /// </summary>
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 질문 타입
    /// </summary>
    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 선택지 (있는 경우)
    /// </summary>
    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// 사용자 입력 필요 여부
    /// </summary>
    [JsonPropertyName("requires_user_input")]
    public bool RequiresUserInput { get; set; } = true;
}

/// <summary>
/// 오류 응답
/// </summary>
public class ErrorResponse : BaseLLMResponse
{
    /// <summary>
    /// 오류 메시지
    /// </summary>
    [JsonPropertyName("error_message")]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 오류 코드
    /// </summary>
    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// 복구 제안
    /// </summary>
    [JsonPropertyName("recovery_suggestions")]
    public List<string> RecoverySuggestions { get; set; } = new();

    /// <summary>
    /// 재시도 가능 여부
    /// </summary>
    [JsonPropertyName("can_retry")]
    public bool CanRetry { get; set; }

    public ErrorResponse()
    {
        Success = false;
    }
}

/// <summary>
/// 특수 기능 응답
/// </summary>
public class SpecialFunctionResponse : BaseLLMResponse
{
    /// <summary>
    /// 기능 이름
    /// </summary>
    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// 기능 파라미터
    /// </summary>
    [JsonPropertyName("function_parameters")]
    public Dictionary<string, object> FunctionParameters { get; set; } = new();

    /// <summary>
    /// 실행 우선순위
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// 특수 처리 필요 여부
    /// </summary>
    [JsonPropertyName("requires_special_handling")]
    public bool RequiresSpecialHandling { get; set; } = true;
}