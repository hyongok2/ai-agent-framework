using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Tools.Abstractions;

/// <summary>
/// Tool 실행 결과 인터페이스
/// </summary>
public interface IToolResult : IResult
{
    /// <summary>
    /// Tool 이름
    /// </summary>
    string ToolName { get; }

    /// <summary>
    /// 실행 결과 데이터
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// 결과를 JSON 문자열로 변환
    /// </summary>
    /// <returns>JSON 문자열</returns>
    string ToJson();
}
