
using AIAgentFramework.Core.Tools.Abstractions;

namespace AIAgentFramework.Core.Tools.Models;

/// <summary>
/// 도구 결과 구현
/// </summary>
public class ToolResult : IToolResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    
    /// <inheritdoc />
    public Dictionary<string, object> Data { get; set; } = new();
    
    /// <inheritdoc />
    public string? ErrorMessage { get; set; }
    
    /// <inheritdoc />
    public TimeSpan ExecutionTime { get; set; }
    
    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 성공 결과 생성
    /// </summary>
    /// <param name="data">결과 데이터</param>
    /// <param name="executionTime">실행 시간</param>
    /// <returns>성공 결과</returns>
    public static ToolResult CreateSuccess(Dictionary<string, object>? data = null, TimeSpan executionTime = default)
    {
        return new ToolResult
        {
            Success = true,
            Data = data ?? new Dictionary<string, object>(),
            ExecutionTime = executionTime
        };
    }

    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="executionTime">실행 시간</param>
    /// <returns>실패 결과</returns>
    public static ToolResult CreateFailure(string errorMessage, TimeSpan executionTime = default)
    {
        return new ToolResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ExecutionTime = executionTime
        };
    }

    /// <summary>
    /// 데이터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 결과</returns>
    public ToolResult WithData(string key, object value)
    {
        Data[key] = value;
        return this;
    }

    /// <summary>
    /// 메타데이터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 결과</returns>
    public ToolResult WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}