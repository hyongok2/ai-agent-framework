

using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Core.Orchestration.Execution;
using AIAgentFramework.Core.User;

namespace AIAgentFramework.Orchestration;

/// <summary>
/// 오케스트레이션 컨텍스트 구현
/// </summary>
public class OrchestrationContext : IOrchestrationContext, IExecutionContext
{
    /// <inheritdoc />
    public string SessionId { get; }

    /// <inheritdoc />
    public string UserRequest { get; }
    
    /// <inheritdoc />
    public IUserRequest OriginalRequest { get; }
    
    /// <inheritdoc />
    public DateTime StartedAt => StartTime;
    
    /// <inheritdoc />
    public DateTime? CompletedAt { get; set; }

    /// <inheritdoc />
    public bool IsCompleted { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; private set; }

    /// <inheritdoc />
    public List<IExecutionStep> ExecutionHistory { get; }

    /// <inheritdoc />
    public Dictionary<string, object> SharedData { get; }

    /// <inheritdoc />
    public IRegistry Registry { get; }

    /// <inheritdoc />
    public DateTime StartTime { get; }

    /// <inheritdoc />
    public DateTime? EndTime { get; private set; }

    /// <inheritdoc />
    public TimeSpan TotalExecutionTime => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="userRequest">사용자 요청</param>
    /// <param name="registry">레지스트리</param>
    public OrchestrationContext(string userRequest, IRegistry registry)
    {
        SessionId = Guid.NewGuid().ToString();
        UserRequest = userRequest ?? throw new ArgumentNullException(nameof(userRequest));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        OriginalRequest = new UserRequest { Content = userRequest };
        IsCompleted = false;
        ExecutionHistory = new List<IExecutionStep>();
        SharedData = new Dictionary<string, object>();
        StartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 생성자 (세션 ID 지정)
    /// </summary>
    /// <param name="sessionId">세션 ID</param>
    /// <param name="userRequest">사용자 요청</param>
    /// <param name="registry">레지스트리</param>
    public OrchestrationContext(string sessionId, string userRequest, IRegistry registry)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        UserRequest = userRequest ?? throw new ArgumentNullException(nameof(userRequest));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        OriginalRequest = new UserRequest { Content = userRequest };
        IsCompleted = false;
        ExecutionHistory = new List<IExecutionStep>();
        SharedData = new Dictionary<string, object>();
        StartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 실행 단계 추가
    /// </summary>
    /// <param name="step">실행 단계</param>
    public void AddExecutionStep(IExecutionStep step)
    {
        if (step == null) throw new ArgumentNullException(nameof(step));
        ExecutionHistory.Add(step);
    }

    /// <summary>
    /// 공유 데이터 설정
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    public void SetSharedData(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));
        SharedData[key] = value;
    }

    /// <summary>
    /// 공유 데이터 조회
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="key">키</param>
    /// <returns>값</returns>
    public T? GetSharedData<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;
        
        if (SharedData.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        
        return default;
    }

    /// <summary>
    /// 오류 설정
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    public void SetError(string errorMessage)
    {
        ErrorMessage = errorMessage;
        IsCompleted = true;
        EndTime = DateTime.UtcNow;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 완료 처리
    /// </summary>
    public void Complete()
    {
        IsCompleted = true;
        EndTime = DateTime.UtcNow;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 마지막 실행 단계 조회
    /// </summary>
    /// <returns>마지막 실행 단계</returns>
    public IExecutionStep? GetLastExecutionStep()
    {
        return ExecutionHistory.LastOrDefault();
    }

    /// <summary>
    /// 성공한 실행 단계 수 조회
    /// </summary>
    /// <returns>성공한 단계 수</returns>
    public int GetSuccessfulStepCount()
    {
        return ExecutionHistory.Count(step => (step as ExecutionStep)?.Success == true);
    }

    /// <summary>
    /// 실패한 실행 단계 수 조회
    /// </summary>
    /// <returns>실패한 단계 수</returns>
    public int GetFailedStepCount()
    {
        return ExecutionHistory.Count(step => (step as ExecutionStep)?.Success == false);
    }
}