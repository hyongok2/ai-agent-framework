using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Tools.Models;

/// <summary>
/// Tool 계약 기본 구현
/// </summary>
public class ToolContract : IToolContract
{
    public string InputSchema { get; init; } = "{}";
    public string OutputSchema { get; init; } = "{}";
    public bool RequiresParameters { get; init; }

    public ToolContract() { }

    public ToolContract(bool requiresParameters)
    {
        RequiresParameters = requiresParameters;
    }

    public bool ValidateInput(object? input)
    {
        // 간단한 구현: null 체크만
        if (RequiresParameters && input == null)
        {
            return false;
        }

        return true;
    }
}
