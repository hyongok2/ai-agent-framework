namespace AIAgentFramework.LLM.Services.ToolSelection;

/// <summary>
/// Tool 선택 서비스의 입력 데이터
/// </summary>
public class ToolSelectionInput
{
    public string UserRequest { get; init; } = string.Empty;
    public string? Context { get; init; }
    public string? History { get; init; }
}
