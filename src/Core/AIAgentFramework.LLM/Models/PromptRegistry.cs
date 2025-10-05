using System.Text.Json;
using AIAgentFramework.LLM.Abstractions;

namespace AIAgentFramework.LLM.Models;

/// <summary>
/// 프롬프트 레지스트리 구현
/// JSON 파일 기반 프롬프트 관리
/// </summary>
public class PromptRegistry : IPromptRegistry
{
    private readonly Dictionary<string, PromptDefinition> _prompts = new();
    private readonly string _templatesBasePath;

    public PromptRegistry(string templatesBasePath)
    {
        _templatesBasePath = templatesBasePath;
        LoadRegistryIfExists();
    }

    public void Register(PromptDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new ArgumentException("프롬프트 이름은 필수입니다.", nameof(definition));
        }

        _prompts[definition.Name] = definition;
    }

    public IPromptTemplate? GetPrompt(string name)
    {
        if (!_prompts.TryGetValue(name, out var definition))
        {
            return null;
        }

        return LoadPromptTemplate(definition);
    }

    public IPromptTemplate? GetPromptByRole(LLMRole role)
    {
        var definition = _prompts.Values.FirstOrDefault(p => p.Role == role);
        return definition == null ? null : LoadPromptTemplate(definition);
    }

    public IReadOnlyCollection<PromptDefinition> GetAllPrompts()
    {
        return _prompts.Values.ToList().AsReadOnly();
    }

    public ValidationResult ValidateVariables(
        string promptName,
        IReadOnlyDictionary<string, object> variables)
    {
        if (!_prompts.TryGetValue(promptName, out var definition))
        {
            throw new ArgumentException($"프롬프트를 찾을 수 없습니다: {promptName}");
        }

        var missingVariables = FindMissingVariables(definition, variables);

        return new ValidationResult
        {
            IsValid = missingVariables.Count == 0,
            MissingVariables = missingVariables
        };
    }

    private void LoadRegistryIfExists()
    {
        var registryPath = Path.Combine(_templatesBasePath, "_registry.json");

        if (!File.Exists(registryPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(registryPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var registry = JsonSerializer.Deserialize<PromptRegistryFile>(json, options);

            if (registry?.Prompts != null)
            {
                foreach (var prompt in registry.Prompts)
                {
                    Register(prompt);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"레지스트리 파일 로드 실패: {ex.Message}", ex);
        }
    }

    private IPromptTemplate LoadPromptTemplate(PromptDefinition definition)
    {
        var templatePath = Path.Combine(_templatesBasePath, definition.TemplatePath);
        return PromptTemplate.FromFile(templatePath, definition.Role);
    }

    private List<string> FindMissingVariables(
        PromptDefinition definition,
        IReadOnlyDictionary<string, object> variables)
    {
        return definition.RequiredVariables
            .Where(v => !variables.ContainsKey(v))
            .ToList();
    }

    private class PromptRegistryFile
    {
        public List<PromptDefinition> Prompts { get; set; } = new();
    }
}
