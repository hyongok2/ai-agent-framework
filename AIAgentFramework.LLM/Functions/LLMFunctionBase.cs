using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.LLM.Functions;

/// <summary>
/// LLM 기능 기본 추상 클래스
/// </summary>
public abstract class LLMFunctionBase : ILLMFunction
{
    protected readonly ILLMProvider _llmProvider;
    protected readonly IPromptManager _promptManager;
    protected readonly ILogger _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="llmProvider">LLM Provider</param>
    /// <param name="promptManager">프롬프트 관리자</param>
    /// <param name="logger">로거</param>
    protected LLMFunctionBase(ILLMProvider llmProvider, IPromptManager promptManager, ILogger logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _promptManager = promptManager ?? throw new ArgumentNullException(nameof(promptManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Role { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public virtual async Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Starting LLM function execution: {FunctionName}", Name);

            // 입력 검증
            if (!await ValidateAsync(context, cancellationToken))
            {
                return LLMResult.CreateFailure($"Validation failed for {Name}", context.Model ?? _llmProvider.DefaultModel);
            }

            // 전처리
            await PreProcessAsync(context, cancellationToken);

            // 프롬프트 준비
            var prompt = await PreparePromptAsync(context, cancellationToken);

            // LLM 호출
            var model = context.Model ?? _llmProvider.DefaultModel;
            var response = await _llmProvider.GenerateAsync(prompt, model, cancellationToken);

            // 후처리
            var result = await PostProcessAsync(response, context, cancellationToken);

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            _logger.LogDebug("Completed LLM function execution: {FunctionName} in {ElapsedMs}ms", 
                Name, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing LLM function: {FunctionName}", Name);
            
            return LLMResult.CreateFailure($"Execution failed: {ex.Message}", context.Model ?? _llmProvider.DefaultModel)
                .WithMetadata("exception", ex.GetType().Name)
                .WithMetadata("execution_time", stopwatch.Elapsed);
        }
    }

    /// <inheritdoc />
    public virtual Task<bool> ValidateAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        // 기본 검증: 필수 파라미터 확인
        var requiredParameters = GetRequiredParameters();
        
        foreach (var parameter in requiredParameters)
        {
            if (!context.Parameters.ContainsKey(parameter) || 
                context.Parameters[parameter] == null ||
                string.IsNullOrWhiteSpace(context.Parameters[parameter].ToString()))
            {
                _logger.LogWarning("Required parameter missing: {Parameter} for function: {FunctionName}", 
                    parameter, Name);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// 필수 파라미터 목록 반환
    /// </summary>
    /// <returns>필수 파라미터 목록</returns>
    protected abstract List<string> GetRequiredParameters();

    /// <summary>
    /// 전처리 (하위 클래스에서 오버라이드 가능)
    /// </summary>
    /// <param name="context">LLM 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    protected virtual Task PreProcessAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 프롬프트 준비 (하위 클래스에서 구현)
    /// </summary>
    /// <param name="context">LLM 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>준비된 프롬프트</returns>
    protected abstract Task<string> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 후처리 (하위 클래스에서 구현)
    /// </summary>
    /// <param name="response">LLM 응답</param>
    /// <param name="context">LLM 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>처리된 결과</returns>
    protected abstract Task<LLMResult> PostProcessAsync(string response, ILLMContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 공통 파라미터 준비
    /// </summary>
    /// <param name="context">LLM 컨텍스트</param>
    /// <returns>프롬프트 파라미터</returns>
    protected virtual Dictionary<string, object> PreparePromptParameters(ILLMContext context)
    {
        var parameters = new Dictionary<string, object>(context.Parameters);
        
        // 공통 파라미터 추가
        parameters["session_id"] = context.SessionId;
        parameters["function_name"] = Name;
        parameters["role"] = Role;
        
        // 실행 이력이 있으면 컨텍스트로 추가
        if (context.ExecutionHistory.Any())
        {
            var historyText = string.Join("\n", context.ExecutionHistory
                .TakeLast(5) // 최근 5개만
                .Select(step => $"- {step.StepType}: {step.Description}"));
            parameters["execution_history"] = historyText;
        }
        else
        {
            parameters["execution_history"] = "No previous execution history";
        }

        // 공유 데이터가 있으면 컨텍스트로 추가
        if (context.SharedData.Any())
        {
            parameters["shared_context"] = string.Join(", ", context.SharedData
                .Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }
        else
        {
            parameters["shared_context"] = "No shared context available";
        }

        return parameters;
    }

    /// <summary>
    /// 토큰 수 계산
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <param name="model">모델</param>
    /// <returns>토큰 수</returns>
    protected async Task<int> CountTokensAsync(string text, string? model = null)
    {
        try
        {
            return await _llmProvider.CountTokensAsync(text, model);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count tokens, using estimation");
            // 간단한 추정: 단어 수 * 1.3
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return (int)(wordCount * 1.3);
        }
    }
}