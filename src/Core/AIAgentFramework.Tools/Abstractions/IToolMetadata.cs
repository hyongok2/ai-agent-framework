namespace AIAgentFramework.Tools.Abstractions;

/// <summary>
/// Tool 메타데이터 인터페이스
/// Tool의 자기 설명 정보
/// </summary>
public interface IToolMetadata
{
    /// <summary>
    /// Tool의 고유 이름 (LLM이 호출할 때 사용)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Tool의 설명 (LLM Plan에서 사용)
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Tool 버전
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Tool 유형 (BuiltIn, PlugIn, MCP)
    /// </summary>
    ToolType Type { get; }
}

/// <summary>
/// Tool 유형
/// </summary>
public enum ToolType
{
    /// <summary>
    /// 내장 도구
    /// </summary>
    BuiltIn,

    /// <summary>
    /// 플러그인 도구
    /// </summary>
    PlugIn,

    /// <summary>
    /// MCP 도구
    /// </summary>
    MCP
}
