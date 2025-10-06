namespace AIAgentFramework.LLM.Services.ParameterGeneration;

/// <summary>
/// 파라미터 생성 입력 데이터
/// </summary>
public class ParameterGenerationInput
{
    /// <summary>
    /// 사용자의 원본 요청
    /// </summary>
    public required string UserRequest { get; init; }

    /// <summary>
    /// 파라미터를 생성할 Tool 이름
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Tool의 입력 스키마 (JSON Schema)
    /// </summary>
    public required string ToolInputSchema { get; init; }

    /// <summary>
    /// 현재 단계 설명
    /// </summary>
    public required string StepDescription { get; init; }

    /// <summary>
    /// 이전 단계들의 실행 결과 (선택적)
    /// </summary>
    public string? PreviousResults { get; init; }

    /// <summary>
    /// 추가 컨텍스트 (선택적)
    /// </summary>
    public string? AdditionalContext { get; init; }
}
