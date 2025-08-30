using AIAgentFramework.ErrorHandling.Exceptions;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.ErrorHandling;

public class ErrorHandlingMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            return await operation();
        }
        catch (LLMException ex)
        {
            _logger.LogError(ex, "LLM 오류 발생 - 모델: {ModelName}, 기능: {FunctionName}, 작업: {OperationName}", 
                ex.ModelName, ex.FunctionName, operationName);
            throw;
        }
        catch (ToolException ex)
        {
            _logger.LogError(ex, "도구 오류 발생 - 도구: {ToolName}, 타입: {ToolType}, 작업: {OperationName}", 
                ex.ToolName, ex.ToolType, operationName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "시스템 오류 발생 - 작업: {OperationName}", operationName);
            throw;
        }
    }

    public ErrorCategory CategorizeError(Exception exception)
    {
        return exception switch
        {
            LLMException => ErrorCategory.LLM,
            ToolException => ErrorCategory.Tool,
            ArgumentException => ErrorCategory.Validation,
            TimeoutException => ErrorCategory.Timeout,
            _ => ErrorCategory.System
        };
    }

    public string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            LLMException llmEx => $"AI 모델 처리 중 오류가 발생했습니다: {llmEx.Message}",
            ToolException toolEx => $"도구 실행 중 오류가 발생했습니다: {toolEx.Message}",
            ArgumentException => "입력 값이 올바르지 않습니다.",
            TimeoutException => "요청 처리 시간이 초과되었습니다.",
            _ => "시스템 오류가 발생했습니다."
        };
    }
}

public enum ErrorCategory
{
    LLM,
    Tool,
    Validation,
    Timeout,
    System
}