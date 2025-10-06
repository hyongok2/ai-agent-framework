using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Models;
using System.Runtime.CompilerServices;

namespace AIAgentFramework.LLM.Services;

/// <summary>
/// LLM 기능의 기본 추상 클래스
/// ILLMFunction을 구현하며 타입 안전성을 제공합니다.
/// </summary>
public abstract class LLMFunctionBase<TInput, TOutput> : ILLMFunction
{
    protected readonly IPromptRegistry PromptRegistry;
    protected readonly ILLMProvider LLMProvider;
    protected readonly LLMFunctionOptions Options;

    protected LLMFunctionBase(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null)
    {
        PromptRegistry = promptRegistry ?? throw new ArgumentNullException(nameof(promptRegistry));
        LLMProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        Options = options ?? LLMFunctionOptions.Default;
    }

    public abstract LLMRole Role { get; }

    public abstract string Description { get; }

    public virtual bool SupportsStreaming => Options.EnableStreaming;

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

    public virtual async IAsyncEnumerable<ILLMStreamChunk> ExecuteStreamAsync(
        ILLMContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!SupportsStreaming)
        {
            throw new NotSupportedException($"{Role} does not support streaming");
        }

        var input = ExtractInput(context);
        var variables = PrepareVariables(input);
        var validation = ValidateVariables(variables);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        var prompt = LoadPrompt();
        var rendered = prompt.Render(variables);

        var index = 0;
        var accumulatedTokens = 0;
        var accumulatedResponse = new System.Text.StringBuilder();

        await foreach (var chunk in CallLLMStreamAsync(rendered, cancellationToken))
        {
            accumulatedTokens += chunk.Length / 4; // 대략적인 토큰 추정
            accumulatedResponse.Append(chunk);

            yield return new LLMStreamChunk
            {
                Index = index++,
                Content = chunk,
                IsFinal = false,
                AccumulatedTokens = accumulatedTokens,
                ParsedResult = null
            };
        }

        // 마지막 청크: 파싱된 결과 포함
        var rawResponse = accumulatedResponse.ToString();
        var parsedOutput = ParseResponse(rawResponse);

        yield return new LLMStreamChunk
        {
            Index = index,
            Content = string.Empty,
            IsFinal = true,
            AccumulatedTokens = accumulatedTokens,
            ParsedResult = parsedOutput
        };
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
        return await LLMProvider.CallAsync(prompt, Options.ModelName, cancellationToken);
    }

    protected virtual IAsyncEnumerable<string> CallLLMStreamAsync(string prompt, CancellationToken cancellationToken)
    {
        return LLMProvider.CallStreamAsync(prompt, Options.ModelName, cancellationToken);
    }
}
