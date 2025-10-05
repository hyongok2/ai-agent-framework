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

    public ToolContract(
        bool requiresParameters,
        string? inputSchema = null,
        string? outputSchema = null)
    {
        RequiresParameters = requiresParameters;
        InputSchema = inputSchema ?? "{}";
        OutputSchema = outputSchema ?? "{}";
    }

    public bool ValidateInput(object? input)
    {
        if (RequiresParameters && input == null)
        {
            return false;
        }

        return true;
    }
}
