using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Models;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AIAgentFramework.LLM.Services;

/// <summary>
/// LLM 기능의 기본 추상 클래스
/// 타입 안전한 Input/Output을 사용하여 명확한 API를 제공합니다.
/// </summary>
public abstract class LLMFunctionBase<TInput, TOutput> : ILLMFunction<TInput, TOutput>
{
    protected readonly IPromptRegistry PromptRegistry;
    protected readonly ILLMProvider LLMProvider;
    protected readonly LLMFunctionOptions Options;
    protected readonly ILogger? Logger;

    protected LLMFunctionBase(
        IPromptRegistry promptRegistry,
        ILLMProvider llmProvider,
        LLMFunctionOptions? options = null,
        ILogger? logger = null)
    {
        PromptRegistry = promptRegistry ?? throw new ArgumentNullException(nameof(promptRegistry));
        LLMProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        Options = options ?? LLMFunctionOptions.Default;
        Logger = logger;
    }

    public abstract LLMRole Role { get; }

    public abstract string Description { get; }

    public virtual bool SupportsStreaming => Options.EnableStreaming;

    /// <summary>
    /// ILLMFunction base interface 구현 (비제네릭 - 동적 호출용)
    /// </summary>
    public async Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        var input = ExtractInput(context);
        return await ExecuteAsync(input, cancellationToken);
    }

    /// <summary>
    /// ILLMFunction base interface 구현 (비제네릭 스트리밍)
    /// </summary>
    public async IAsyncEnumerable<ILLMStreamChunk> ExecuteStreamAsync(
        ILLMContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var input = ExtractInput(context);
        await foreach (var chunk in ExecuteStreamAsync(input, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// 타입 안전한 Execute (ILLMFunction&lt;TInput, TOutput&gt; 구현)
    /// </summary>
    public async Task<ILLMResult> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var executionId = Guid.NewGuid().ToString("N")[..8];
        ILLMResult? result = null;
        Exception? error = null;

        try
        {
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

            result = CreateResult(rawResponse, output);
            return result;
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            sw.Stop();
            await LogExecutionAsync(executionId, input, result, sw.ElapsedMilliseconds, error, cancellationToken);
        }
    }

    private async Task LogExecutionAsync(
        string executionId,
        TInput input,
        ILLMResult? result,
        long durationMs,
        Exception? error,
        CancellationToken cancellationToken)
    {
        if (Logger == null) return;

        try
        {
            var logEntry = LogEntry.Create(
                logType: "LLM",
                targetName: Role.ToString(),
                executionId: executionId,
                timestamp: DateTimeOffset.UtcNow,
                request: input,
                response: result,
                durationMs: durationMs,
                success: error == null,
                errorMessage: error?.Message
            );

            await Logger.LogAsync(logEntry, cancellationToken);
        }
        catch
        {
            // 로깅 실패는 조용히 무시
        }
    }

    public virtual IAsyncEnumerable<ILLMStreamChunk> ExecuteStreamAsync(
        TInput input,
        CancellationToken cancellationToken = default)
    {
        return ExecuteStreamWithLoggingAsync(input, cancellationToken);
    }

    private async IAsyncEnumerable<ILLMStreamChunk> ExecuteStreamWithLoggingAsync(
        TInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!SupportsStreaming)
        {
            throw new NotSupportedException($"{Role} does not support streaming");
        }

        var sw = Stopwatch.StartNew();
        var executionId = Guid.NewGuid().ToString("N")[..8];

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
        ILLMResult? result = null;

        await foreach (var chunk in CallLLMStreamAsync(rendered, cancellationToken))
        {
            accumulatedTokens += chunk.Length / 4;
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
        result = CreateResult(rawResponse, parsedOutput);

        yield return new LLMStreamChunk
        {
            Index = index,
            Content = string.Empty,
            IsFinal = true,
            AccumulatedTokens = accumulatedTokens,
            ParsedResult = parsedOutput
        };

        // 스트리밍 완료 후 로깅 (예외는 상위에서 처리)
        sw.Stop();
        _ = Task.Run(async () =>
        {
            await LogExecutionAsync(executionId, input, result, sw.ElapsedMilliseconds, null, CancellationToken.None);
        });
    }

    /// <summary>
    /// ILLMContext로부터 TInput 추출 (비제네릭 호출 지원)
    /// </summary>
    protected abstract TInput ExtractInput(ILLMContext context);

    /// <summary>
    /// Input을 프롬프트 변수로 변환
    /// </summary>
    protected abstract IReadOnlyDictionary<string, object> PrepareVariables(TInput input);

    /// <summary>
    /// LLM 응답을 Output으로 파싱
    /// </summary>
    protected abstract TOutput ParseResponse(string response);

    /// <summary>
    /// 최종 Result 생성
    /// </summary>
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
