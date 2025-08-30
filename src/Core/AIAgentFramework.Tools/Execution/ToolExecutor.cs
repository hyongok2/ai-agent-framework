using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Tools.Execution;

/// <summary>
/// 도구 실행기 구현
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly IRegistry _registry;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(IRegistry registry, ILogger<ToolExecutor> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IToolResult> ExecuteDirectAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return ToolResult.CreateFailure("도구 이름이 필요합니다.");

        var tool = _registry.GetTool(toolName);
        if (tool == null)
            return ToolResult.CreateFailure($"도구 '{toolName}'을 찾을 수 없습니다.");

        try
        {
            var emptyInput = new ToolInput();
            return await tool.ExecuteAsync(emptyInput, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "도구 '{ToolName}' 직접 실행 중 오류 발생", toolName);
            return ToolResult.CreateFailure($"도구 실행 오류: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IToolResult> ExecuteWithParametersAsync(string toolName, IToolInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return ToolResult.CreateFailure("도구 이름이 필요합니다.");

        if (input == null)
            return ToolResult.CreateFailure("입력 파라미터가 필요합니다.");

        var tool = _registry.GetTool(toolName);
        if (tool == null)
            return ToolResult.CreateFailure($"도구 '{toolName}'을 찾을 수 없습니다.");

        try
        {
            var isValid = await tool.ValidateAsync(input, cancellationToken);
            if (!isValid)
                return ToolResult.CreateFailure("입력 파라미터 검증에 실패했습니다.");

            return await tool.ExecuteAsync(input, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "도구 '{ToolName}' 파라미터 실행 중 오류 발생", toolName);
            return ToolResult.CreateFailure($"도구 실행 오류: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IToolResult> ExecuteWithLLMParametersAsync(string toolName, string userRequest, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return ToolResult.CreateFailure("도구 이름이 필요합니다.");

        if (string.IsNullOrWhiteSpace(userRequest))
            return ToolResult.CreateFailure("사용자 요청이 필요합니다.");

        var tool = _registry.GetTool(toolName);
        if (tool == null)
            return ToolResult.CreateFailure($"도구 '{toolName}'을 찾을 수 없습니다.");

        try
        {
            // LLM Tool Parameter Setter 조회
            var parameterSetter = _registry.GetLLMFunction("tool_parameter_setter");
            if (parameterSetter == null)
                return ToolResult.CreateFailure("LLM Tool Parameter Setter를 찾을 수 없습니다.");

            // LLM을 통해 파라미터 생성
            var llmContext = new LLMContext
            {
                UserRequest = userRequest,
                ToolName = toolName,
                ToolContract = tool.Contract
            };

            var llmResult = await parameterSetter.ExecuteAsync(llmContext, cancellationToken);
            if (!llmResult.Success)
                return ToolResult.CreateFailure($"파라미터 생성 실패: {llmResult.ErrorMessage}");

            // 생성된 파라미터로 도구 실행
            var toolInput = CreateToolInputFromLLMResult(llmResult);
            return await ExecuteWithParametersAsync(toolName, toolInput, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "도구 '{ToolName}' LLM 파라미터 실행 중 오류 발생", toolName);
            return ToolResult.CreateFailure($"도구 실행 오류: {ex.Message}");
        }
    }

    private IToolInput CreateToolInputFromLLMResult(ILLMResult llmResult)
    {
        var parameters = new Dictionary<string, object>();
        
        if (llmResult.Data.ContainsKey("parameters"))
        {
            if (llmResult.Data["parameters"] is Dictionary<string, object> paramDict)
            {
                parameters = paramDict;
            }
        }

        return new ToolInput { Parameters = parameters };
    }
}