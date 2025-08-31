using AIAgentFramework.Core.LLM.Abstractions;

namespace AIAgentFramework.Core.LLM.Models;

/// <summary>
/// LLM 결과 구현
/// </summary>
public class LLMResult : ILLMResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    
    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string? ErrorMessage { get; set; }
    
    /// <inheritdoc />
    public int TokensUsed { get; set; }
    
    /// <inheritdoc />
    public string Model { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public TimeSpan ExecutionTime { get; set; }
    
    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <inheritdoc />
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// 성공 결과 생성
    /// </summary>
    /// <param name="content">생성된 내용</param>
    /// <param name="model">사용된 모델</param>
    /// <param name="tokensUsed">사용된 토큰 수</param>
    /// <param name="executionTime">실행 시간</param>
    /// <returns>성공 결과</returns>
    public static LLMResult CreateSuccess(string content, string model, int tokensUsed = 0, TimeSpan executionTime = default)
    {
        return new LLMResult
        {
            Success = true,
            Content = content,
            Model = model,
            TokensUsed = tokensUsed,
            ExecutionTime = executionTime
        };
    }

    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="model">사용된 모델</param>
    /// <returns>실패 결과</returns>
    public static LLMResult CreateFailure(string errorMessage, string model = "")
    {
        return new LLMResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Model = model
        };
    }

    /// <summary>
    /// 메타데이터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 결과</returns>
    public LLMResult WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}