using AIAgentFramework.LLM.Services.Universal;

namespace AIAgentFramework.LLM.Services.Planning;

/// <summary>
/// 실행 계획의 개별 단계
/// </summary>
public class TaskStep
{
    /// <summary>
    /// 단계 번호 (1부터 시작)
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// 단계 설명
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 사용할 Tool 이름
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Tool 파라미터 (JSON 문자열)
    /// </summary>
    public required string Parameters { get; init; }

    /// <summary>
    /// 이 단계의 출력을 저장할 변수명 (선택적)
    /// 예: "fileList", "summary" 등
    /// </summary>
    public string? OutputVariable { get; init; }

    /// <summary>
    /// 의존하는 이전 단계 번호들 (선택적)
    /// </summary>
    public List<int> DependsOn { get; init; } = new();

    /// <summary>
    /// 예상 실행 시간 (초)
    /// </summary>
    public int? EstimatedSeconds { get; init; }

    /// <summary>
    /// UniversalLLM 사용 시 ResponseGuide (선택적)
    /// ToolName이 "UniversalLLM"인 경우 사용
    /// </summary>
    public ResponseGuide? ResponseGuide { get; init; }
}
