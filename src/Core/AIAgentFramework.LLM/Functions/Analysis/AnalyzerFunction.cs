

using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Attributes;
using AIAgentFramework.Core.LLM.Models;
using AIAgentFramework.Registry;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.LLM.Functions.Analysis;

/// <summary>
/// 분석 및 평가 LLM 기능
/// </summary>
[LLMFunction(
    name: "Analyzer",
    role: "analyzer",
    description: "데이터, 텍스트, 상황을 분석하고 인사이트를 제공하는 LLM 기능",
    SupportedModels = "gpt-4,gpt-4-turbo,claude-3-opus,claude-3-sonnet",
    RequiredParameters = "analysis_target,analysis_type",
    OptionalParameters = "context,criteria,output_format,depth_level",
    Tags = "analysis,evaluation,insights,reasoning",
    Priority = 90
)]
public class AnalyzerFunction : LLMFunctionBase
{
    /// <inheritdoc />
    public override string Name => "Analyzer";

    /// <inheritdoc />
    public override string Role => "analyzer";

    /// <inheritdoc />
    public override string Description => "데이터, 텍스트, 상황을 분석하고 인사이트를 제공하는 LLM 기능";

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="llmProvider">LLM Provider</param>
    /// <param name="promptManager">프롬프트 관리자</param>
    /// <param name="logger">로거</param>
    /// <param name="registry">Registry</param>
    public AnalyzerFunction(ILLMProvider llmProvider, IPromptManager promptManager, ILogger<AnalyzerFunction> logger, IAdvancedRegistry registry)
        : base(llmProvider, promptManager, logger, registry)
    {
    }

    /// <inheritdoc />
    protected override List<string> GetRequiredParameters()
    {
        return new List<string> { "analysis_target", "analysis_type" };
    }

    /// <inheritdoc />
    protected override async Task<string> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        var parameters = PreparePromptParameters(context);
        
        // 기본값 설정
        if (!parameters.ContainsKey("context"))
        {
            parameters["context"] = "일반적인 분석 - 특별한 컨텍스트 없음";
        }
        
        if (!parameters.ContainsKey("criteria"))
        {
            var analysisType = parameters["analysis_type"]?.ToString()?.ToLowerInvariant();
            parameters["criteria"] = analysisType switch
            {
                "sentiment" => "감정 상태, 긍정/부정 정도, 감정 강도",
                "content" => "주요 주제, 핵심 메시지, 논리적 구조, 설득력",
                "performance" => "효율성, 속도, 정확성, 리소스 사용률",
                "quality" => "완성도, 일관성, 오류율, 사용자 만족도",
                "risk" => "위험 요인, 가능성, 영향도, 완화 방안",
                "opportunity" => "기회 요소, 잠재 가치, 실현 가능성",
                _ => "관련성, 중요도, 영향력, 실행 가능성"
            };
        }
        
        if (!parameters.ContainsKey("output_format"))
        {
            parameters["output_format"] = "structured_json";
        }
        
        if (!parameters.ContainsKey("depth_level"))
        {
            parameters["depth_level"] = "detailed";
        }

        return await _promptManager.LoadPromptAsync("analyzer", parameters, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<LLMResult> PostProcessAsync(string response, ILLMContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = context.Model ?? _llmProvider.DefaultModel;
            var tokensUsed = await CountTokensAsync(response, model);

            // JSON 파싱 시도
            AnalysisResponse? analysisResponse = null;
            try
            {
                analysisResponse = JsonSerializer.Deserialize<AnalysisResponse>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse analysis response as JSON, treating as plain text");
            }

            var result = LLMResult.CreateSuccess(response, model, tokensUsed)
                .WithMetadata("function_type", "analyzer")
                .WithMetadata("analysis_type", context.Parameters["analysis_type"]?.ToString() ?? "unknown")
                .WithMetadata("has_structured_data", analysisResponse != null);

            if (analysisResponse != null)
            {
                // 구조화된 데이터 메타데이터 추가
                result.WithMetadata("analysis_summary", analysisResponse.Summary)
                      .WithMetadata("confidence_score", analysisResponse.ConfidenceScore)
                      .WithMetadata("findings_count", analysisResponse.KeyFindings.Count)
                      .WithMetadata("recommendations_count", analysisResponse.Recommendations.Count);

                // 분석 결과를 공유 데이터에 저장
                context.SharedData["analysis_result"] = analysisResponse;
                context.SharedData["analysis_insights"] = analysisResponse.KeyFindings;
                context.SharedData["analysis_recommendations"] = analysisResponse.Recommendations;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post-process analysis response");
            return LLMResult.CreateFailure($"Post-processing failed: {ex.Message}", context.Model ?? _llmProvider.DefaultModel);
        }
    }

    /// <inheritdoc />
    public override async Task<bool> ValidateAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        // 기본 검증
        if (!await base.ValidateAsync(context, cancellationToken))
            return false;

        // 분석 대상 검증
        var analysisTarget = context.Parameters["analysis_target"]?.ToString();
        if (string.IsNullOrWhiteSpace(analysisTarget))
        {
            _logger.LogWarning("Analysis target is required");
            return false;
        }

        // 분석 타입 검증
        var analysisType = context.Parameters["analysis_type"]?.ToString();
        if (string.IsNullOrWhiteSpace(analysisType))
        {
            _logger.LogWarning("Analysis type is required");
            return false;
        }

        // 지원하는 분석 타입 확인
        var supportedTypes = new[] { "sentiment", "content", "performance", "quality", "risk", "opportunity", "general" };
        if (!supportedTypes.Contains(analysisType.ToLowerInvariant()))
        {
            _logger.LogWarning("Unsupported analysis type: {AnalysisType}", analysisType);
            return false;
        }

        return true;
    }
}