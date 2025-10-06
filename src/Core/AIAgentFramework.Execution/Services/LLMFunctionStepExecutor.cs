using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.Execution.Models;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Services.Planning;
using AIAgentFramework.LLM.Services.Summarization;

namespace AIAgentFramework.Execution.Services;

/// <summary>
/// LLM Function 실행 담당
/// </summary>
public class LLMFunctionStepExecutor : IStepExecutor
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    public async Task<StepExecutionResult> ExecuteAsync(
        TaskStep step,
        ExecutableItem executable,
        string? parameters,
        string userRequest,
        IAgentContext agentContext,
        Action<string>? onStreamChunk = null,
        CancellationToken cancellationToken = default)
    {
        if (executable.Type != ExecutableType.LLMFunction || executable.LLMFunction == null)
        {
            throw new ArgumentException("ExecutableItem must be an LLM Function", nameof(executable));
        }

        var llmFunction = executable.LLMFunction;

        // AgentContext로부터 LLMContext 생성
        var llmContext = LLMContext.FromAgentContext(agentContext, userRequest);

        // Parameters가 있으면 추가 파라미터로 병합
        if (!string.IsNullOrEmpty(parameters))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(parameters);
                var mergedParams = new Dictionary<string, object>(llmContext.Parameters);

                // JSON 객체의 각 속성을 실제 값으로 변환해서 추가
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    var value = ConvertJsonElement(property.Value);
                    mergedParams[property.Name] = value;
                }

                llmContext = new LLMContext
                {
                    UserInput = userRequest,
                    Parameters = mergedParams,
                    ExecutionId = llmContext.ExecutionId,
                    UserId = llmContext.UserId,
                    SessionId = llmContext.SessionId
                };
            }
            catch (Exception ex)
            {
                // JSON 파싱 실패하면 parameters 무시
                System.Console.WriteLine($"[WARNING] Parameters 파싱 실패: {ex.Message}");
            }
        }

        // 스트리밍 지원 여부 확인
        object? parsedData = null;
        string? rawResponse = null;
        bool isSuccess = true;
        string? errorMessage = null;

        if (llmFunction.SupportsStreaming && onStreamChunk != null)
        {
            // 스트리밍 모드로 실행 (콜백이 제공된 경우에만)
            var accumulatedResponse = new System.Text.StringBuilder();

            await foreach (var chunk in llmFunction.ExecuteStreamAsync(llmContext, cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    onStreamChunk(chunk.Content); // 콜백으로 청크 전달
                    accumulatedResponse.Append(chunk.Content);
                }

                if (chunk.IsFinal && chunk.ParsedResult != null)
                {
                    parsedData = chunk.ParsedResult;
                }
            }

            rawResponse = accumulatedResponse.ToString();
        }
        else
        {
            // 일반 모드로 실행
            var llmResult = await llmFunction.ExecuteAsync(llmContext, cancellationToken);
            parsedData = llmResult.ParsedData;
            rawResponse = llmResult.RawResponse;
            isSuccess = llmResult.IsSuccess;
            errorMessage = llmResult.ErrorMessage;
        }

        // LLM Function 결과를 JSON으로 직렬화
        string? output = null;
        if (parsedData != null)
        {
            output = JsonSerializer.Serialize(parsedData, _jsonOptions);

            // 파싱된 결과에서 성공/오류 정보 추출 (SummarizationResult 등)
            if (parsedData is SummarizationResult summaryResult)
            {
                isSuccess = string.IsNullOrEmpty(summaryResult.ErrorMessage);
                errorMessage = summaryResult.ErrorMessage;
            }
        }
        else
        {
            output = rawResponse;
        }

        return new StepExecutionResult
        {
            StepNumber = step.StepNumber,
            Description = step.Description,
            ToolName = step.ToolName,
            IsSuccess = isSuccess,
            Output = output,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// JsonElement를 실제 C# 타입으로 변환
    /// </summary>
    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.ToString()
        };
    }
}
