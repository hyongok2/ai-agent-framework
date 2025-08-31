namespace AIAgentFramework.Core.Actions.Models;

/// <summary>
/// 액션 실행 결과
/// </summary>
public class ActionResult
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 실행 결과 데이터
    /// </summary>
    public object? Data { get; set; }
    
    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 실행 시간
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 성공 결과 생성
    /// </summary>
    public static ActionResult Success(object? data = null, TimeSpan executionTime = default)
    {
        return new ActionResult
        {
            IsSuccess = true,
            Data = data,
            ExecutionTime = executionTime
        };
    }

    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    public static ActionResult Failure(string errorMessage, TimeSpan executionTime = default)
    {
        return new ActionResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExecutionTime = executionTime
        };
    }
}