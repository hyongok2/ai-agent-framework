namespace AIAgentFramework.LLM.Services.ParameterGeneration;

/// <summary>
/// 파라미터 생성 결과
/// </summary>
public class ParameterGenerationResult
{
    /// <summary>
    /// Tool 이름
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// 생성된 파라미터 (JSON 문자열 또는 단순 값)
    /// </summary>
    public required string Parameters { get; init; }

    /// <summary>
    /// 파라미터 생성 근거/설명
    /// </summary>
    public string? Reasoning { get; init; }

    /// <summary>
    /// 파라미터 생성 성공 여부
    /// </summary>
    public bool IsValid { get; init; } = true;

    /// <summary>
    /// 실패 시 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }
}
