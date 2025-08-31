using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.Orchestration.Execution;
using AIAgentFramework.Core.Tools.Abstractions;

namespace AIAgentFramework.Core.LLM.Models;

/// <summary>
/// LLM 컨텍스트 구현
/// </summary>
public class LLMContext : ILLMContext
{
    /// <inheritdoc />
    public string SessionId { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string? Model { get; set; }
    
    /// <inheritdoc />
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <inheritdoc />
    public List<IExecutionStep> ExecutionHistory { get; set; } = new();
    
    /// <inheritdoc />
    public Dictionary<string, object> SharedData { get; set; } = new();
    
    /// <summary>
    /// 사용자 요청
    /// </summary>
    public string? UserRequest { get; set; }
    
    /// <summary>
    /// 도구 이름
    /// </summary>
    public string? ToolName { get; set; }
    
    /// <summary>
    /// 도구 계약
    /// </summary>
    public IToolContract? ToolContract { get; set; }

    /// <summary>
    /// 생성자
    /// </summary>
    public LLMContext()
    {
        SessionId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="sessionId">세션 ID</param>
    public LLMContext(string sessionId)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
    }

    /// <summary>
    /// 파라미터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 컨텍스트</returns>
    public LLMContext WithParameter(string key, object value)
    {
        Parameters[key] = value;
        return this;
    }

    /// <summary>
    /// 여러 파라미터 추가
    /// </summary>
    /// <param name="parameters">파라미터 딕셔너리</param>
    /// <returns>현재 컨텍스트</returns>
    public LLMContext WithParameters(Dictionary<string, object> parameters)
    {
        foreach (var kvp in parameters)
        {
            Parameters[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// 모델 설정
    /// </summary>
    /// <param name="model">모델명</param>
    /// <returns>현재 컨텍스트</returns>
    public LLMContext WithModel(string model)
    {
        Model = model;
        return this;
    }

    /// <summary>
    /// 메타데이터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 컨텍스트</returns>
    public LLMContext WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}