
using AIAgentFramework.Core.Tools.Abstractions;

namespace AIAgentFramework.Core.Tools.Models;

/// <summary>
/// 도구 입력 구현
/// </summary>
public class ToolInput : IToolInput
{
    /// <inheritdoc />
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <inheritdoc />
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// 파라미터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 입력</returns>
    public ToolInput WithParameter(string key, object value)
    {
        Parameters[key] = value;
        return this;
    }

    /// <summary>
    /// 여러 파라미터 추가
    /// </summary>
    /// <param name="parameters">파라미터 딕셔너리</param>
    /// <returns>현재 입력</returns>
    public ToolInput WithParameters(Dictionary<string, object> parameters)
    {
        foreach (var kvp in parameters)
        {
            Parameters[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// 메타데이터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 입력</returns>
    public ToolInput WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// 컨텍스트 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 입력</returns>
    public ToolInput WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }
}