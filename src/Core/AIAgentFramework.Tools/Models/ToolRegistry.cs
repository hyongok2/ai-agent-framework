using System.Text.Json;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Tools.Models;

/// <summary>
/// Tool Registry 구현체
/// 모든 Tool을 중앙에서 관리
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public void Register(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        lock (_lock)
        {
            if (_tools.ContainsKey(tool.Metadata.Name))
            {
                throw new InvalidOperationException(
                    $"Tool '{tool.Metadata.Name}'은(는) 이미 등록되어 있습니다.");
            }

            _tools.Add(tool.Metadata.Name, tool);
        }
    }

    public ITool? GetTool(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_lock)
        {
            return _tools.GetValueOrDefault(name);
        }
    }

    public IReadOnlyCollection<ITool> GetAllTools()
    {
        lock (_lock)
        {
            return _tools.Values.ToList().AsReadOnly();
        }
    }

    public IReadOnlyCollection<ITool> GetToolsByType(ToolType type)
    {
        lock (_lock)
        {
            return _tools.Values
                .Where(t => t.Metadata.Type == type)
                .ToList()
                .AsReadOnly();
        }
    }

    public string GetToolDescriptionsForLLM()
    {
        lock (_lock)
        {
            var descriptions = _tools.Values
                .Select(CreateToolDescription)
                .ToList();

            return JsonSerializer.Serialize(descriptions, CreateJsonOptions());
        }
    }

    private static object CreateToolDescription(ITool tool)
    {
        return new
        {
            name = tool.Metadata.Name,
            description = tool.Metadata.Description,
            type = tool.Metadata.Type.ToString(),
            requiresParameters = tool.Contract.RequiresParameters,
            inputSchema = tool.Contract.InputSchema,
            outputSchema = tool.Contract.OutputSchema
        };
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
