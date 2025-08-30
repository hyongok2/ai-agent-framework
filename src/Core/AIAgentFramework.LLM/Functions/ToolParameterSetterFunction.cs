using AIAgentFramework.Core.Attributes;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.LLM.Functions;

/// <summary>
/// 도구 파라미터 설정 LLM 기능
/// </summary>
[LLMFunction("tool_parameter_setter", "도구 실행을 위한 파라미터를 설정합니다", "tool_parameter_setter")]
public class ToolParameterSetterFunction : LLMFunctionBase
{
    public ToolParameterSetterFunction(ILLMProvider llmProvider, IPromptManager promptManager, ILogger<ToolParameterSetterFunction> logger) 
        : base(llmProvider, promptManager, logger)
    {
    }
    public override string Name => "tool_parameter_setter";
    public override string Description => "도구 실행을 위한 파라미터를 설정합니다";
    public override string Role => "tool_parameter_setter";

    protected override List<string> GetRequiredParameters()
    {
        return new List<string> { "user_request", "tool_name", "tool_contract" };
    }

    protected override async Task<string> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        return await BuildPromptAsync(context);
    }

    protected override Task<LLMResult> PostProcessAsync(string response, ILLMContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((LLMResult)ParseToolParameterResponse(response, context));
    }
    public override async Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = await BuildPromptAsync(context);
            var response = await _llmProvider.GenerateAsync(prompt, context.Model ?? _llmProvider.DefaultModel, cancellationToken);
            
            return ParseToolParameterResponse(response, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "도구 파라미터 설정 중 오류 발생");
            return (LLMResult)LLMResult.CreateFailure($"파라미터 설정 실패: {ex.Message}");
        }
    }

    private async Task<string> BuildPromptAsync(ILLMContext context)
    {
        var toolName = context.Parameters.GetValueOrDefault("tool_name", "")?.ToString() ?? "";
        var userRequest = context.Parameters.GetValueOrDefault("user_request", "")?.ToString() ?? "";
        var toolContract = context.Parameters.GetValueOrDefault("tool_contract") as IToolContract;
        
        var prompt = $@"사용자 요청에 따라 도구 실행에 필요한 파라미터를 생성하세요.

도구 이름: {toolName}
사용자 요청: {userRequest}

도구 스키마:
입력 스키마: {toolContract?.InputSchema ?? "정보 없음"}
필수 파라미터: {string.Join(", ", toolContract?.RequiredParameters ?? new List<string>())}

지시사항:
1. 사용자 요청을 분석하여 도구 실행에 필요한 파라미터를 추출하세요
2. 필수 파라미터는 반드시 포함해야 합니다
3. 적절한 기본값을 설정하세요
4. 아래 JSON 형식으로 응답하세요

응답 형식:
{{
  ""success"": true,
  ""parameters"": {{
    ""param1"": ""value1"",
    ""param2"": ""value2""
  }},
  ""reasoning"": ""파라미터 설정 이유""
}}";

        return await Task.FromResult(prompt);
    }

    private LLMResult ParseToolParameterResponse(string response, ILLMContext context)
    {
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
            
            if (!jsonResponse.TryGetProperty("success", out var successElement) || 
                !successElement.GetBoolean())
            {
                return (LLMResult)LLMResult.CreateFailure("LLM이 파라미터 생성에 실패했습니다.");
            }

            var result = new LLMResult
            {
                Success = true,
                Data = new Dictionary<string, object>()
            };

            if (jsonResponse.TryGetProperty("parameters", out var parametersElement))
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersElement.GetRawText());
                result.Data["parameters"] = parameters ?? new Dictionary<string, object>();
            }

            if (jsonResponse.TryGetProperty("reasoning", out var reasoningElement))
            {
                result.Data["reasoning"] = reasoningElement.GetString() ?? "";
            }

            result.Data["tool_name"] = context.ToolName ?? "";
            result.Data["user_request"] = context.UserRequest ?? "";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "도구 파라미터 응답 파싱 중 오류 발생");
            return (LLMResult)LLMResult.CreateFailure($"응답 파싱 실패: {ex.Message}");
        }
    }
}