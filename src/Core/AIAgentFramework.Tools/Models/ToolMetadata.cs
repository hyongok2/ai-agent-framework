using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Tools.Models;

/// <summary>
/// Tool 메타데이터 기본 구현
/// </summary>
public class ToolMetadata : IToolMetadata
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public ToolType Type { get; init; }

    public ToolMetadata() { }

    public ToolMetadata(string name, string description, ToolType type)
    {
        Name = name;
        Description = description;
        Type = type;
    }
}
