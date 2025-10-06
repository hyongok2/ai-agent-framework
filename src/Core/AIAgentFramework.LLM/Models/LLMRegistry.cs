using System.Text;
using System.Text.Json;
using AIAgentFramework.LLM.Abstractions;

namespace AIAgentFramework.LLM.Models;

/// <summary>
/// LLM 기능 Registry 구현체
/// </summary>
public class LLMRegistry : ILLMRegistry
{
    private readonly Dictionary<LLMRole, ILLMFunction> _functions = new();
    private readonly Dictionary<string, ILLMProvider> _providers = new();
    private readonly object _lock = new();

    public void Register(ILLMFunction function)
    {
        if (function == null)
            throw new ArgumentNullException(nameof(function));

        lock (_lock)
        {
            if (_functions.ContainsKey(function.Role))
            {
                throw new InvalidOperationException($"LLM Function with role '{function.Role}' is already registered");
            }

            _functions[function.Role] = function;
        }
    }

    public ILLMFunction? GetFunction(LLMRole role)
    {
        lock (_lock)
        {
            return _functions.TryGetValue(role, out var function) ? function : null;
        }
    }

    public IReadOnlyCollection<ILLMFunction> GetAllFunctions()
    {
        lock (_lock)
        {
            return _functions.Values.ToList().AsReadOnly();
        }
    }

    public void RegisterProvider(ILLMProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        lock (_lock)
        {
            if (_providers.ContainsKey(provider.ProviderName))
            {
                throw new InvalidOperationException($"LLM Provider '{provider.ProviderName}' is already registered");
            }

            _providers[provider.ProviderName] = provider;
        }
    }

    public ILLMProvider? GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return null;

        lock (_lock)
        {
            return _providers.TryGetValue(providerName, out var provider) ? provider : null;
        }
    }

    public string GetFunctionDescriptionsForLLM()
    {
        lock (_lock)
        {
            if (_functions.Count == 0)
            {
                return "[]";
            }

            var descriptions = _functions.Values.Select(f => new
            {
                role = f.Role.ToString(),
                name = f.Role.ToString(),
                description = f.Description,
                supportsStreaming = f.SupportsStreaming
            });

            return JsonSerializer.Serialize(descriptions, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
