namespace AIAgentFramework.Configuration.Models;

/// <summary>
/// LLM 설정 모델
/// </summary>
public class LLMConfiguration
{
    /// <summary>
    /// 기본 LLM 제공자
    /// </summary>
    public string DefaultProvider { get; set; } = "openai";
    
    /// <summary>
    /// 기능별 모델 매핑
    /// </summary>
    public Dictionary<string, string> Models { get; set; } = new()
    {
        ["planner"] = "gpt-4",
        ["interpreter"] = "gpt-3.5-turbo",
        ["summarizer"] = "claude-3-sonnet",
        ["generator"] = "gpt-4",
        ["evaluator"] = "gpt-4",
        ["rewriter"] = "gpt-3.5-turbo",
        ["explainer"] = "gpt-4",
        ["reasoner"] = "gpt-4",
        ["converter"] = "gpt-3.5-turbo",
        ["visualizer"] = "gpt-4",
        ["tool_parameter_setter"] = "gpt-3.5-turbo",
        ["dialogue_manager"] = "gpt-4",
        ["knowledge_retriever"] = "claude-3-sonnet",
        ["meta_manager"] = "gpt-4"
    };
    
    /// <summary>
    /// 제공자별 설정
    /// </summary>
    public Dictionary<string, ProviderConfiguration> Providers { get; set; } = new();
    
    /// <summary>
    /// 기본 파라미터
    /// </summary>
    public LLMParameters DefaultParameters { get; set; } = new();
}

/// <summary>
/// LLM 제공자 설정
/// </summary>
public class ProviderConfiguration
{
    /// <summary>
    /// API 키
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API 엔드포인트
    /// </summary>
    public string? Endpoint { get; set; }
    
    /// <summary>
    /// 조직 ID (OpenAI)
    /// </summary>
    public string? OrganizationId { get; set; }
    
    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// LLM 파라미터
/// </summary>
public class LLMParameters
{
    /// <summary>
    /// 최대 토큰 수
    /// </summary>
    public int MaxTokens { get; set; } = 1000;
    
    /// <summary>
    /// 온도 (창의성)
    /// </summary>
    public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// Top-p 샘플링
    /// </summary>
    public double TopP { get; set; } = 1.0;
    
    /// <summary>
    /// 빈도 페널티
    /// </summary>
    public double FrequencyPenalty { get; set; } = 0.0;
    
    /// <summary>
    /// 존재 페널티
    /// </summary>
    public double PresencePenalty { get; set; } = 0.0;
}