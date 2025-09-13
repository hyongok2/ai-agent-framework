using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Models;
using AIAgentFramework.LLM.Services;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.LLM.Functions;

/// <summary>
/// 텍스트 생성 Function - Generator 역할
/// 사용자의 요청에 따라 다양한 형태의 텍스트를 생성합니다.
/// </summary>
public class GeneratorFunction : LLMFunctionBase
{
    private const string TEMPLATE_NAME = "Functions/generator";

    private const string FALLBACK_TEMPLATE = """
        당신은 전문적인 텍스트 생성 AI입니다.
        사용자의 요청을 정확히 이해하고, 고품질의 텍스트를 생성해주세요.

        ## 요청사항
        {UserRequest}

        ## 추가 컨텍스트
        {AdditionalContext}

        ## 생성 지침
        - 요청의 의도를 정확히 파악하세요
        - 명확하고 읽기 쉬운 문장을 작성하세요
        - 필요시 구조화된 형태로 제공하세요
        - 전문적이고 신뢰할 수 있는 내용을 생성하세요

        응답:
        """;

    private readonly PromptTemplateService _templateService;

    public GeneratorFunction(ILLMProvider llmProvider, ILogger<GeneratorFunction> logger, PromptTemplateService? templateService = null)
        : base(llmProvider, logger)
    {
        _templateService = templateService ?? new PromptTemplateService(logger);
    }

    public override string Name => "generator";
    public override string Role => "generator";
    public override string Description => "사용자 요청에 따라 고품질 텍스트를 생성하는 기능";

    public override async Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generator function started for session: {SessionId}", context.SessionId);

            // 입력 검증
            var validationResult = await ValidateInputAsync(context, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Generator function validation failed: {Error}", validationResult.ErrorMessage);
                return LLMResult.CreateFailure(validationResult.ErrorMessage!, context.Model ?? _llmProvider.DefaultModel);
            }

            // 프롬프트 생성
            var prompt = await BuildPromptAsync(context, cancellationToken);
            _logger.LogDebug("Generated prompt for Generator function: {PromptLength} characters", prompt.Length);

            // LLM 요청 실행
            var model = context.Model ?? _llmProvider.DefaultModel;
            var response = await _llmProvider.GenerateAsync(prompt, model, cancellationToken);

            _logger.LogInformation("Generator function completed successfully for session: {SessionId}", context.SessionId);
            return LLMResult.CreateSuccess(response, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generator function failed for session: {SessionId}", context.SessionId);
            return LLMResult.CreateFailure($"텍스트 생성 중 오류가 발생했습니다: {ex.Message}", context.Model ?? _llmProvider.DefaultModel);
        }
    }

    public override async Task<bool> ValidateAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        var result = await ValidateInputAsync(context, cancellationToken);
        return result.IsValid;
    }

    private async Task<ValidationResult> ValidateInputAsync(ILLMContext context, CancellationToken cancellationToken)
    {
        // 기본 컨텍스트 검증
        if (context == null)
            return ValidationResult.Failed("컨텍스트가 null입니다");

        if (string.IsNullOrWhiteSpace(context.SessionId))
            return ValidationResult.Failed("세션 ID가 필요합니다");

        if (string.IsNullOrWhiteSpace(context.UserRequest))
            return ValidationResult.Failed("사용자 요청이 없습니다");

        // 모델 검증
        var model = context.Model ?? _llmProvider.DefaultModel;
        if (!_llmProvider.SupportedModels.Contains(model))
        {
            _logger.LogWarning("Unsupported model requested: {Model}, using default: {DefaultModel}",
                model, _llmProvider.DefaultModel);
        }

        // 요청 길이 검증 (너무 길면 토큰 제한 초과 가능)
        if (context.UserRequest.Length > 10000)
            return ValidationResult.Failed("요청이 너무 깁니다 (최대 10,000자)");

        await Task.CompletedTask; // 비동기 작업을 위한 placeholder
        return ValidationResult.Success();
    }

    private async Task<string> BuildPromptAsync(ILLMContext context, CancellationToken cancellationToken)
    {
        try
        {
            // 템플릿 변수 준비
            var variables = new Dictionary<string, string>
            {
                ["UserRequest"] = context.UserRequest ?? "일반적인 텍스트 생성",
                ["AdditionalContext"] = BuildAdditionalContext(context)
            };

            // 파일 기반 템플릿 로드 및 처리
            var prompt = await _templateService.LoadAndProcessTemplateAsync(TEMPLATE_NAME, variables);
            _logger.LogDebug("Template loaded successfully from file: {TemplateName}", TEMPLATE_NAME);

            return prompt;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load template file, using fallback template: {TemplateName}", TEMPLATE_NAME);

            // 파일 로드 실패 시 폴백 템플릿 사용
            var variables = new Dictionary<string, string>
            {
                ["UserRequest"] = context.UserRequest ?? "일반적인 텍스트 생성",
                ["AdditionalContext"] = BuildAdditionalContext(context)
            };

            return _templateService.ProcessTemplate(FALLBACK_TEMPLATE, variables);
        }
    }

    private string BuildAdditionalContext(ILLMContext context)
    {
        var contextParts = new List<string>();

        if (context.Metadata?.Count > 0)
        {
            foreach (var metadata in context.Metadata)
            {
                contextParts.Add($"- {metadata.Key}: {metadata.Value}");
            }
        }

        if (contextParts.Count == 0)
        {
            contextParts.Add("추가 컨텍스트 없음");
        }

        return string.Join("\n", contextParts);
    }

    /// <summary>
    /// 검증 결과를 나타내는 클래스
    /// </summary>
    private class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }

        private ValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new(true);
        public static ValidationResult Failed(string errorMessage) => new(false, errorMessage);
    }
}