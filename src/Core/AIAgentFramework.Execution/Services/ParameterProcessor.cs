using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Services.ParameterGeneration;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Execution.Services;

/// <summary>
/// 파라미터 처리 구현체 (변수 치환 + 검증 + 자동 생성)
/// </summary>
public class ParameterProcessor : IParameterProcessor
{
    private readonly ParameterGeneratorFunction _parameterGenerator;

    public ParameterProcessor(ParameterGeneratorFunction parameterGenerator)
    {
        _parameterGenerator = parameterGenerator ?? throw new ArgumentNullException(nameof(parameterGenerator));
    }

    public async Task<ParameterProcessingResult> ProcessAsync(
        ITool? tool,
        string? rawParameters,
        string userRequest,
        string stepDescription,
        IAgentContext agentContext,
        CancellationToken cancellationToken = default)
    {
        // 1. 변수 치환
        var parameters = SubstituteVariables(rawParameters, agentContext);

        // 2. 검증 (Tool만 해당)
        if (tool != null && tool.Contract.RequiresParameters)
        {
            var validationResult = ValidateParameters(parameters);
            if (!validationResult.IsValid)
            {
                // 3. 자동 생성
                var generatedParams = await GenerateParametersAsync(
                    tool,
                    userRequest,
                    stepDescription,
                    agentContext,
                    cancellationToken);

                if (!generatedParams.IsSuccess)
                {
                    return new ParameterProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = generatedParams.ErrorMessage
                    };
                }

                parameters = generatedParams.ProcessedParameters;
            }
        }

        return new ParameterProcessingResult
        {
            IsSuccess = true,
            ProcessedParameters = parameters
        };
    }

    private string? SubstituteVariables(string? parameters, IAgentContext agentContext)
    {
        if (string.IsNullOrEmpty(parameters) || !agentContext.Variables.Any())
        {
            return parameters;
        }

        var result = parameters;

        // JSON 문자열 값 내부의 변수만 매칭: "...{variable}..." 패턴
        // 큰따옴표 안에 있는 {variable} 또는 {variable.property} 형식만 찾기
        var pathPattern = new System.Text.RegularExpressions.Regex(@"""[^""]*?\{([^}]+)\}[^""]*?""");
        var matches = pathPattern.Matches(result);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            // match.Value는 전체 문자열 (예: "content": "{upperContent}")
            // 내부의 {variable} 패턴만 추출
            var innerPattern = new System.Text.RegularExpressions.Regex(@"\{([^}]+)\}");
            var innerMatches = innerPattern.Matches(match.Value);

            foreach (System.Text.RegularExpressions.Match innerMatch in innerMatches)
            {
                var fullPath = innerMatch.Groups[1].Value; // 예: "fileContent.Content"
                var parts = fullPath.Split('.');
                var key = parts[0]; // 예: "fileContent"

                if (!agentContext.Contains(key))
                {
                    continue;
                }

                var value = agentContext.Get<object>(key)?.ToString() ?? string.Empty;

                // JSON 경로가 있는 경우 (예: variable.property)
                if (parts.Length > 1)
                {
                    value = ExtractJsonProperty(value, parts[1]);
                }
                else
                {
                    // 단순 변수인 경우, JSON이면 Content 속성 자동 추출
                    value = ExtractContentFromJson(value);
                }

                // JSON 문자열 안에 들어가는 경우 이스케이프
                if (result.StartsWith("{") && result.Contains("\""))
                {
                    value = EscapeForJson(value);
                }

                result = result.Replace(innerMatch.Value, value);
            }
        }

        return result;
    }

    /// <summary>
    /// JSON 문자열에서 특정 속성 추출
    /// </summary>
    private string ExtractJsonProperty(string jsonString, string propertyName)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonString);
            if (jsonDoc.RootElement.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind == JsonValueKind.String
                    ? property.GetString() ?? jsonString
                    : property.ToString();
            }
        }
        catch
        {
            // JSON 파싱 실패하면 원본 문자열 사용
        }

        return jsonString;
    }

    /// <summary>
    /// JSON 객체인 경우 의미있는 콘텐츠 속성 자동 추출
    /// 우선순위: TransformedText > Content > Output > Result > 전체 JSON
    /// </summary>
    private string ExtractContentFromJson(string value)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(value);
            var root = jsonDoc.RootElement;

            // 우선순위에 따라 속성 추출 시도
            var candidateProperties = new[] { "TransformedText", "Content", "Output", "Result", "Data" };

            foreach (var propName in candidateProperties)
            {
                if (root.TryGetProperty(propName, out var prop))
                {
                    return prop.ValueKind == JsonValueKind.String
                        ? prop.GetString() ?? value
                        : prop.ToString();
                }
            }

            // 적합한 속성이 없으면 전체 JSON 반환
            return value;
        }
        catch
        {
            // JSON 파싱 실패하면 원본 문자열 사용
        }

        return value;
    }

    private string EscapeForJson(string value)
    {
        // JSON 이스케이프: ", \, 제어문자 등
        var escaped = JsonSerializer.Serialize(value);

        // Serialize는 앞뒤에 따옴표를 추가하므로 제거
        if (escaped.StartsWith("\"") && escaped.EndsWith("\"") && escaped.Length >= 2)
        {
            return escaped.Substring(1, escaped.Length - 2);
        }

        return escaped;
    }

    private (bool IsValid, string? ErrorMessage) ValidateParameters(string? parameters)
    {
        if (string.IsNullOrEmpty(parameters))
        {
            return (false, "Parameters are empty");
        }

        // JSON 파싱 시도로 기본 검증
        try
        {
            if (parameters.StartsWith('{') || parameters.StartsWith('['))
            {
                JsonDocument.Parse(parameters); // 파싱만 해서 유효성 확인
            }
            // 단순 문자열이면 Tool에 그대로 전달
            return (true, null);
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON: {ex.Message}");
        }
    }

    private async Task<ParameterProcessingResult> GenerateParametersAsync(
        ITool tool,
        string userRequest,
        string stepDescription,
        IAgentContext agentContext,
        CancellationToken cancellationToken)
    {
        // AgentContext로부터 LLMContext 생성
        var paramContext = LLMContext.FromAgentContext(agentContext, userRequest);

        // ParameterGenerator용 추가 파라미터 설정
        var llmParams = new Dictionary<string, object>(paramContext.Parameters)
        {
            ["TOOL_NAME"] = tool.Metadata.Name,
            ["TOOL_INPUT_SCHEMA"] = tool.Contract.InputSchema,
            ["STEP_DESCRIPTION"] = stepDescription
        };

        // 이전 단계 결과가 있으면 추가
        if (agentContext.Variables.Any())
        {
            llmParams["PREVIOUS_RESULTS"] = JsonSerializer.Serialize(
                agentContext.Variables,
                new JsonSerializerOptions { WriteIndented = true });
        }

        var contextWithParams = new LLMContext
        {
            UserInput = userRequest,
            Parameters = llmParams,
            ExecutionId = paramContext.ExecutionId,
            UserId = paramContext.UserId,
            SessionId = paramContext.SessionId
        };

        var paramResult = await _parameterGenerator.ExecuteAsync(contextWithParams, cancellationToken);
        var paramGenResult = (ParameterGenerationResult)paramResult.ParsedData!;

        if (!paramGenResult.IsValid)
        {
            return new ParameterProcessingResult
            {
                IsSuccess = false,
                ErrorMessage = $"파라미터 생성 실패: {paramGenResult.ErrorMessage}"
            };
        }

        return new ParameterProcessingResult
        {
            IsSuccess = true,
            ProcessedParameters = paramGenResult.Parameters
        };
    }
}
