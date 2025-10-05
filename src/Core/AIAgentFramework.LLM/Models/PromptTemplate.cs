using System.Text.RegularExpressions;
using AIAgentFramework.LLM.Abstractions;

namespace AIAgentFramework.LLM.Models;

/// <summary>
/// 프롬프트 템플릿 구현
/// {{VARIABLE}} 형식의 변수 치환 지원
/// </summary>
public class PromptTemplate : IPromptTemplate
{
    private static readonly Regex VariablePattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public LLMRole Role { get; init; }
    public string Template { get; init; } = string.Empty;
    public IReadOnlyList<string> Variables { get; init; }

    private PromptTemplate()
    {
        Variables = Array.Empty<string>();
    }

    public static PromptTemplate FromText(string template, LLMRole role)
    {
        var variables = ExtractVariables(template);

        return new PromptTemplate
        {
            Template = template,
            Role = role,
            Variables = variables
        };
    }

    public static PromptTemplate FromFile(string filePath, LLMRole role)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"프롬프트 템플릿 파일을 찾을 수 없습니다: {filePath}");
        }

        var template = File.ReadAllText(filePath);
        return FromText(template, role);
    }

    public string Render(IReadOnlyDictionary<string, object> parameters)
    {
        var result = Template;

        foreach (var variable in Variables)
        {
            if (parameters.TryGetValue(variable, out var value))
            {
                var placeholder = $"{{{{{variable}}}}}";
                result = result.Replace(placeholder, value?.ToString() ?? string.Empty);
            }
        }

        return result;
    }

    private static IReadOnlyList<string> ExtractVariables(string template)
    {
        var matches = VariablePattern.Matches(template);
        return matches.Select(m => m.Groups[1].Value).Distinct().ToList();
    }
}
