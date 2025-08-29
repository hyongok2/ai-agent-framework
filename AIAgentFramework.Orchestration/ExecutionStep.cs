using AIAgentFramework.Core.Interfaces;

namespace AIAgentFramework.Orchestration;

/// <summary>
/// 실행 단계 구현
/// </summary>
public class ExecutionStep : IExecutionStep
{
    /// <inheritdoc />
    public string StepId { get; }

    /// <inheritdoc />
    public string StepType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc />
    public object Input { get; set; } = string.Empty;

    /// <inheritdoc />
    public object Output { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string FunctionName { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public DateTime ExecutedAt { get; set; }
    
    /// <inheritdoc />
    public TimeSpan Duration => ExecutionTime;
    
    /// <inheritdoc />
    public bool IsSuccess => Success;

    /// <inheritdoc />
    public bool Success { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public DateTime StartTime { get; set; }

    /// <inheritdoc />
    public DateTime? EndTime { get; set; }

    /// <inheritdoc />
    public TimeSpan ExecutionTime { get; set; }

    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    public ExecutionStep()
    {
        StepId = Guid.NewGuid().ToString();
        StartTime = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="stepType">단계 타입</param>
    /// <param name="description">설명</param>
    public ExecutionStep(string stepType, string description) : this()
    {
        StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>
    /// 메타데이터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 단계</returns>
    public ExecutionStep WithMetadata(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Metadata[key] = value;
        }
        return this;
    }

    /// <summary>
    /// 입력 설정
    /// </summary>
    /// <param name="input">입력</param>
    /// <returns>현재 단계</returns>
    public ExecutionStep WithInput(string input)
    {
        Input = input ?? string.Empty;
        return this;
    }

    /// <summary>
    /// 출력 설정
    /// </summary>
    /// <param name="output">출력</param>
    /// <returns>현재 단계</returns>
    public ExecutionStep WithOutput(string output)
    {
        Output = output ?? string.Empty;
        return this;
    }

    /// <summary>
    /// 성공 처리
    /// </summary>
    /// <returns>현재 단계</returns>
    public ExecutionStep MarkAsSuccess()
    {
        Success = true;
        EndTime = DateTime.UtcNow;
        ExecutionTime = EndTime.Value.Subtract(StartTime);
        return this;
    }

    /// <summary>
    /// 실패 처리
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <returns>현재 단계</returns>
    public ExecutionStep MarkAsFailure(string errorMessage)
    {
        Success = false;
        ErrorMessage = errorMessage;
        EndTime = DateTime.UtcNow;
        ExecutionTime = EndTime.Value.Subtract(StartTime);
        return this;
    }

    /// <summary>
    /// 실행 시간 계산
    /// </summary>
    /// <returns>실행 시간</returns>
    public TimeSpan CalculateExecutionTime()
    {
        var endTime = EndTime ?? DateTime.UtcNow;
        return endTime.Subtract(StartTime);
    }

    /// <summary>
    /// 단계 요약 정보 생성
    /// </summary>
    /// <returns>요약 정보</returns>
    public string GetSummary()
    {
        var status = Success ? "성공" : "실패";
        var duration = CalculateExecutionTime().TotalMilliseconds;
        return $"[{StepType}] {Description} - {status} ({duration:F0}ms)";
    }
}