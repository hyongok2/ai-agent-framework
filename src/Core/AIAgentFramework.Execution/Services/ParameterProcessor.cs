using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Services.ParameterGeneration;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Execution.Services;

/// <summary>
/// 파라미터 처리 구현체 (LLM 기반 지능형 파라미터 생성)
/// </summary>
public class ParameterProcessor : IParameterProcessor
{
    private readonly ParameterGeneratorFunction _parameterGenerator;

    public ParameterProcessor(ParameterGeneratorFunction parameterGenerator)
    {
        _parameterGenerator = parameterGenerator ?? throw new ArgumentNullException(nameof(parameterGenerator));
    }

    public async Task<ParameterProcessingResult> ProcessAsync(
        string targetName,
        string? inputSchema,
        bool requiresParameters,
        string? rawParameters,
        string userRequest,
        string stepDescription,
        IAgentContext agentContext,
        Action<string>? onStreamChunk = null,
        CancellationToken cancellationToken = default)
    {
        // 파라미터를 필요로 하지 않으면 종료
        if (!requiresParameters)
        {
            return new ParameterProcessingResult
            {
                IsSuccess = true,
                ProcessedParameters = null
            };
        }

        // 1단계: 변수 치환 수행 (AgentContext의 Variables 사용)
        var substitutedParameters = SubstituteVariables(rawParameters, agentContext);

        // 2단계: 파라미터가 완전한지 확인
        if (IsCompleteParameter(substitutedParameters))
        {
            // placeholder가 없고 유효한 파라미터면 그대로 사용
            return new ParameterProcessingResult
            {
                IsSuccess = true,
                ProcessedParameters = substitutedParameters
            };
        }

        // 3단계: placeholder가 있거나 불완전하면 LLM으로 생성
        onStreamChunk?.Invoke($"\n🔧 파라미터 생성 중 (Tool: {targetName})...\n");

        return await GenerateParametersAsync(
            targetName,
            inputSchema ?? "{}",
            userRequest,
            stepDescription,
            agentContext,
            onStreamChunk,
            cancellationToken);
    }

    /// <summary>
    /// 파라미터가 완전한지 확인 (placeholder 없음)
    /// </summary>
    private bool IsCompleteParameter(string? parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return false;
        }

        // {variable_name} 형태의 placeholder 찾기
        // JSON의 {} 와 구분하기 위해 정규식 사용
        // {word} 패턴 중 앞뒤에 따옴표가 없는 것이 placeholder
        var placeholderPattern = @"\{(\w+)\}";
        var matches = System.Text.RegularExpressions.Regex.Matches(parameters, placeholderPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var index = match.Index;
            // 앞에 따옴표가 있으면 JSON 키이므로 스킵
            if (index > 0 && parameters[index - 1] == '"')
            {
                continue;
            }
            // 뒤에 따옴표가 있으면 JSON 값이므로 스킵
            if (index + match.Length < parameters.Length && parameters[index + match.Length] == '"')
            {
                continue;
            }

            // placeholder 발견
            return false;
        }

        return true;
    }


    private async Task<ParameterProcessingResult> GenerateParametersAsync(
        string targetName,
        string inputSchema,
        string userRequest,
        string stepDescription,
        IAgentContext agentContext,
        Action<string>? onStreamChunk,
        CancellationToken cancellationToken)
    {
        // AgentContext의 모든 정보를 LLM에 전달
        var llmParams = new Dictionary<string, object>
        {
            // Tool/LLM Function 정보
            ["TOOL_NAME"] = targetName,
            ["TOOL_INPUT_SCHEMA"] = inputSchema,
            ["STEP_DESCRIPTION"] = stepDescription,

            // 사용자 입력
            ["USER_REQUEST"] = userRequest
        };

        // 이전 단계 결과들 (AgentContext.Variables)
        if (agentContext.Variables.Any())
        {
            var previousResults = new Dictionary<string, object>();

            foreach (var variable in agentContext.Variables)
            {
                previousResults[variable.Key] = variable.Value;
            }

            llmParams["PREVIOUS_RESULTS"] = JsonSerializer.Serialize(
                previousResults,
                new JsonSerializerOptions { WriteIndented = true });
        }

        // 대화 히스토리가 Variables에 있으면 추가
        var history = agentContext.Get<string>("CONVERSATION_HISTORY");
        if (!string.IsNullOrWhiteSpace(history))
        {
            llmParams["CONVERSATION_HISTORY"] = history;
        }

        // 추가 컨텍스트 정보가 Variables에 있으면 추가
        var additionalContext = agentContext.Get<string>("ADDITIONAL_CONTEXT");
        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            llmParams["ADDITIONAL_CONTEXT"] = additionalContext;
        }

        var llmContext = new LLMContext
        {
            UserInput = userRequest,
            Parameters = llmParams,
            ExecutionId = agentContext.ExecutionId,
            UserId = agentContext.UserId,
            SessionId = agentContext.SessionId
        };

        // 스트리밍 지원 시 스트리밍으로 실행
        ParameterGenerationResult? paramGenResult = null;

        if (_parameterGenerator.SupportsStreaming && onStreamChunk != null)
        {
            await foreach (var chunk in _parameterGenerator.ExecuteStreamAsync(llmContext, cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    onStreamChunk(chunk.Content);
                }

                if (chunk.IsFinal && chunk.ParsedResult != null)
                {
                    paramGenResult = (ParameterGenerationResult)chunk.ParsedResult;
                }
            }
        }
        else
        {
            var paramResult = await _parameterGenerator.ExecuteAsync(llmContext, cancellationToken);
            paramGenResult = (ParameterGenerationResult)paramResult.ParsedData!;
        }

        if (paramGenResult == null || !paramGenResult.IsValid)
        {
            return new ParameterProcessingResult
            {
                IsSuccess = false,
                ErrorMessage = $"파라미터 생성 실패: {paramGenResult?.ErrorMessage}"
            };
        }

        onStreamChunk?.Invoke($"\n✅ 파라미터 생성 완료\n");

        return new ParameterProcessingResult
        {
            IsSuccess = true,
            ProcessedParameters = paramGenResult.Parameters
        };
    }

    /// <summary>
    /// 파라미터 문자열의 {변수명} placeholder를 AgentContext의 Variables 값으로 치환
    /// 예: {"content": "{output}"} → {"content": "실제 출력 값"}
    /// </summary>
    private string? SubstituteVariables(string? parameters, IAgentContext agentContext)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return parameters;
        }

        var result = parameters;

        // {변수명} 패턴을 찾아서 치환
        var placeholderPattern = @"\{(\w+)\}";
        var matches = System.Text.RegularExpressions.Regex.Matches(parameters, placeholderPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var variableName = match.Groups[1].Value;
            var placeholder = match.Value; // {variableName}

            // AgentContext에서 변수 값 가져오기
            if (agentContext.Variables.TryGetValue(variableName, out var value))
            {
                var valueStr = value?.ToString() ?? string.Empty;

                // JSON 문자열 내부에서 치환할 때는 이스케이프 처리
                valueStr = System.Text.Json.JsonSerializer.Serialize(valueStr);
                valueStr = valueStr.Trim('"'); // 양쪽 따옴표 제거

                result = result.Replace(placeholder, valueStr);
            }
            // 변수가 없으면 placeholder를 그대로 둠 (LLM이 생성하도록)
        }

        return result;
    }
}
