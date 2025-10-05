namespace AIAgentFramework.LLM.Services.ToolSelection;

/// <summary>
/// Tool 선택 서비스의 출력 데이터
/// </summary>
public class ToolSelectionResult
{
    public string ToolName { get; init; } = string.Empty;
    public string Parameters { get; init; } = string.Empty;
}
