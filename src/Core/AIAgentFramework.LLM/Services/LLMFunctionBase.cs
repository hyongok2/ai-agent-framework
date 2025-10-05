using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Services;

/// <summary>
/// LLM 기능의 기본 추상 클래스
/// ILLMFunction을 구현하며 타입 안전성을 제공합니다.
/// </summary>
public abstract class LLMFunctionBase<TInput, TOutput> : ILLMFunction
{
    protected readonly IPromptRegistry PromptRegistry;
    protected readonly ILLMProvider LLMProvider;

    protected LLMFunctionBase(IPromptRegistry promptRegistry, ILLMProvider llmProvider)
    {
        PromptRegistry = promptRegistry ?? throw new ArgumentNullException(nameof(promptRegistry));
        LLMProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    public abstract LLMRole Role { get; }

    public abstract string Description { get; }

    public virtual bool SupportsStreaming => false;

    public async Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        var input = ExtractInput(context);
        var variables = PrepareVariables(input);
        var validation = ValidateVariables(variables);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        var prompt = LoadPrompt();
        var rendered = prompt.Render(variables);
        var rawResponse = await CallLLMAsync(rendered, cancellationToken);
        var output = ParseResponse(rawResponse);

        return CreateResult(rawResponse, output);
    }

    public virtual IAsyncEnumerable<ILLMStreamChunk> ExecuteStreamAsync(
        ILLMContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"{Role} does not support streaming");
    }

    protected abstract TInput ExtractInput(ILLMContext context);

    protected abstract IReadOnlyDictionary<string, object> PrepareVariables(TInput input);

    protected abstract TOutput ParseResponse(string response);

    protected abstract ILLMResult CreateResult(string rawResponse, TOutput output);

    protected abstract string GetPromptName();

    protected virtual IPromptTemplate LoadPrompt()
    {
        var promptName = GetPromptName();
        return PromptRegistry.GetPrompt(promptName)
            ?? throw new InvalidOperationException($"프롬프트를 찾을 수 없습니다: {promptName}");
    }

    protected virtual ValidationResult ValidateVariables(IReadOnlyDictionary<string, object> variables)
    {
        return PromptRegistry.ValidateVariables(GetPromptName(), variables);
    }

    protected virtual async Task<string> CallLLMAsync(string prompt, CancellationToken cancellationToken)
    {
        return await LLMProvider.CallAsync(prompt, GetModelName(), cancellationToken);
    }

    protected virtual string GetModelName()
    {
        return "gpt-oss:20b";
    }
}
