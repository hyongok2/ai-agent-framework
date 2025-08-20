# Phase 3: 스트리밍 이벤트 구조

## 목표
실시간 스트리밍을 위한 이벤트 청크 구조 구현

## 구현 파일

### 1. Streaming/StreamChunk.cs

```csharp
using System;
using System.Text.Json;
using Agent.Core.Abstractions.Common;

namespace Agent.Core.Abstractions.Streaming;

/// <summary>
/// 스트리밍 이벤트의 기본 타입
/// </summary>
public abstract record StreamChunk
{
    public required RunId RunId { get; init; }
    public required StepId StepId { get; init; }
    public required long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 청크 타입 식별자
    /// </summary>
    public abstract string ChunkType { get; }
}

/// <summary>
/// 텍스트 토큰 청크
/// </summary>
public sealed record TokenChunk : StreamChunk
{
    public required string Text { get; init; }
    public override string ChunkType => "token";
    
    /// <summary>
    /// 토큰이 문장의 끝인지 여부
    /// </summary>
    public bool IsEndOfSentence { get; init; }
}

/// <summary>
/// 도구 호출 청크
/// </summary>
public sealed record ToolCallChunk : StreamChunk
{
    public required string ToolName { get; init; }
    public required JsonDocument Arguments { get; init; }
    public override string ChunkType => "tool_call";
    
    /// <summary>
    /// 도구 호출 ID (추적용)
    /// </summary>
    public string? CallId { get; init; }
}

/// <summary>
/// JSON 부분 청크 (스트리밍 JSON 구성용)
/// </summary>
public sealed record JsonPartialChunk : StreamChunk
{
    public required string PartialJson { get; init; }
    public override string ChunkType => "json_partial";
    
    /// <summary>
    /// JSON 경로 (예: "result.items[0].name")
    /// </summary>
    public string? JsonPath { get; init; }
}

/// <summary>
/// 상태 업데이트 청크
/// </summary>
public sealed record StatusChunk : StreamChunk
{
    public required StatusType Status { get; init; }
    public string? Message { get; init; }
    public override string ChunkType => "status";
    
    /// <summary>
    /// 진행률 (0-100)
    /// </summary>
    public int? ProgressPercentage { get; init; }
}

/// <summary>
/// 최종 결과 청크
/// </summary>
public sealed record FinalChunk : StreamChunk
{
    public required JsonDocument Result { get; init; }
    public bool Success { get; init; } = true;
    public string? Error { get; init; }
    public override string ChunkType => "final";
    
    /// <summary>
    /// 실행 메트릭
    /// </summary>
    public ExecutionMetrics? Metrics { get; init; }
}

/// <summary>
/// 에러 청크
/// </summary>
public sealed record ErrorChunk : StreamChunk
{
    public required string ErrorCode { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
    public override string ChunkType => "error";
    
    /// <summary>
    /// 재시도 가능 여부
    /// </summary>
    public bool IsRetryable { get; init; }
}

/// <summary>
/// 상태 타입
/// </summary>
public enum StatusType
{
    Initializing,
    Started,
    InProgress,
    WaitingForInput,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Timeout
}

/// <summary>
/// 실행 메트릭
/// </summary>
public sealed record ExecutionMetrics
{
    public TimeSpan Duration { get; init; }
    public int TokensUsed { get; init; }
    public int ToolCallsCount { get; init; }
    public int StepsExecuted { get; init; }
    public decimal? Cost { get; init; }
}
```

### 2. Streaming/IStreamAggregator.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Agent.Core.Abstractions.Streaming;

/// <summary>
/// 스트림 청크 집계 인터페이스
/// </summary>
public interface IStreamAggregator
{
    /// <summary>
    /// 청크를 집계에 추가
    /// </summary>
    Task AddChunkAsync(StreamChunk chunk, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 현재까지 집계된 결과 가져오기
    /// </summary>
    Task<AggregatedResult> GetCurrentResultAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 집계 완료 처리
    /// </summary>
    Task<AggregatedResult> FinalizeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 집계된 결과
/// </summary>
public sealed record AggregatedResult
{
    public string? FullText { get; init; }
    public List<ToolCallChunk> ToolCalls { get; init; } = new();
    public JsonDocument? FinalJson { get; init; }
    public StatusType CurrentStatus { get; init; }
    public List<ErrorChunk> Errors { get; init; } = new();
    public ExecutionMetrics? Metrics { get; init; }
}

/// <summary>
/// 스트림 청크 필터
/// </summary>
public interface IStreamFilter
{
    /// <summary>
    /// 청크 필터링
    /// </summary>
    bool ShouldProcess(StreamChunk chunk);
    
    /// <summary>
    /// 청크 변환
    /// </summary>
    StreamChunk? Transform(StreamChunk chunk);
}
```

### 3. Streaming/StreamingExtensions.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Agent.Core.Abstractions.Streaming;

/// <summary>
/// 스트리밍 관련 확장 메서드
/// </summary>
public static class StreamingExtensions
{
    /// <summary>
    /// 특정 타입의 청크만 필터링
    /// </summary>
    public static async IAsyncEnumerable<T> OfType<T>(
        this IAsyncEnumerable<StreamChunk> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) 
        where T : StreamChunk
    {
        await foreach (var chunk in source.WithCancellation(cancellationToken))
        {
            if (chunk is T typedChunk)
                yield return typedChunk;
        }
    }
    
    /// <summary>
    /// 텍스트 청크들을 문자열로 결합
    /// </summary>
    public static async Task<string> CombineTextAsync(
        this IAsyncEnumerable<StreamChunk> source,
        CancellationToken cancellationToken = default)
    {
        var texts = new List<string>();
        
        await foreach (var chunk in source.OfType<TokenChunk>(cancellationToken))
        {
            texts.Add(chunk.Text);
        }
        
        return string.Concat(texts);
    }
    
    /// <summary>
    /// 버퍼링하여 배치로 전달
    /// </summary>
    public static async IAsyncEnumerable<IReadOnlyList<StreamChunk>> Buffer(
        this IAsyncEnumerable<StreamChunk> source,
        int bufferSize,
        TimeSpan maxWait,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new List<StreamChunk>(bufferSize);
        var lastFlush = DateTime.UtcNow;
        
        await foreach (var chunk in source.WithCancellation(cancellationToken))
        {
            buffer.Add(chunk);
            
            var shouldFlush = buffer.Count >= bufferSize || 
                              DateTime.UtcNow - lastFlush >= maxWait;
            
            if (shouldFlush)
            {
                yield return buffer.ToList();
                buffer.Clear();
                lastFlush = DateTime.UtcNow;
            }
        }
        
        if (buffer.Count > 0)
            yield return buffer;
    }
    
    /// <summary>
    /// 에러 발생 시 재시도
    /// </summary>
    public static async IAsyncEnumerable<StreamChunk> WithRetry(
        this IAsyncEnumerable<StreamChunk> source,
        int maxRetries = 3,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        
        while (retryCount <= maxRetries)
        {
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    yield return enumerator.Current;
                }
                break;
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                enumerator = source.GetAsyncEnumerator(cancellationToken);
            }
        }
    }
}
```

### 4. Tests/Streaming/StreamChunkTests.cs

```csharp
using System;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Agent.Core.Abstractions.Common;
using Agent.Core.Abstractions.Streaming;

namespace Agent.Core.Abstractions.Tests.Streaming;

public class StreamChunkTests
{
    private readonly RunId _runId = RunId.New();
    private readonly StepId _stepId = StepId.New(1);
    
    [Fact]
    public void TokenChunk_Should_Have_Correct_Properties()
    {
        // Arrange & Act
        var chunk = new TokenChunk
        {
            RunId = _runId,
            StepId = _stepId,
            Sequence = 1,
            Text = "Hello World",
            IsEndOfSentence = false
        };
        
        // Assert
        chunk.ChunkType.Should().Be("token");
        chunk.Text.Should().Be("Hello World");
        chunk.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void ToolCallChunk_Should_Store_Arguments()
    {
        // Arrange
        var args = JsonDocument.Parse("{\"param1\": \"value1\"}");
        
        // Act
        var chunk = new ToolCallChunk
        {
            RunId = _runId,
            StepId = _stepId,
            Sequence = 2,
            ToolName = "calculator",
            Arguments = args,
            CallId = "call_123"
        };
        
        // Assert
        chunk.ChunkType.Should().Be("tool_call");
        chunk.ToolName.Should().Be("calculator");
        chunk.Arguments.RootElement.GetProperty("param1").GetString().Should().Be("value1");
        chunk.CallId.Should().Be("call_123");
    }
    
    [Fact]
    public void StatusChunk_Should_Support_Progress()
    {
        // Arrange & Act
        var chunk = new StatusChunk
        {
            RunId = _runId,
            StepId = _stepId,
            Sequence = 3,
            Status = StatusType.InProgress,
            Message = "Processing step 2 of 5",
            ProgressPercentage = 40
        };
        
        // Assert
        chunk.ChunkType.Should().Be("status");
        chunk.Status.Should().Be(StatusType.InProgress);
        chunk.ProgressPercentage.Should().Be(40);
    }
    
    [Fact]
    public void FinalChunk_Should_Include_Metrics()
    {
        // Arrange
        var result = JsonDocument.Parse("{\"success\": true}");
        var metrics = new ExecutionMetrics
        {
            Duration = TimeSpan.FromSeconds(5),
            TokensUsed = 1500,
            ToolCallsCount = 3,
            StepsExecuted = 5,
            Cost = 0.025m
        };
        
        // Act
        var chunk = new FinalChunk
        {
            RunId = _runId,
            StepId = _stepId,
            Sequence = 100,
            Result = result,
            Success = true,
            Metrics = metrics
        };
        
        // Assert
        chunk.ChunkType.Should().Be("final");
        chunk.Success.Should().BeTrue();
        chunk.Metrics.Should().NotBeNull();
        chunk.Metrics!.TokensUsed.Should().Be(1500);
        chunk.Metrics.Cost.Should().Be(0.025m);
    }
    
    [Fact]
    public void ErrorChunk_Should_Indicate_Retryability()
    {
        // Arrange & Act
        var chunk = new ErrorChunk
        {
            RunId = _runId,
            StepId = _stepId,
            Sequence = 50,
            ErrorCode = "RATE_LIMIT",
            Message = "Rate limit exceeded",
            Details = "Try again in 60 seconds",
            IsRetryable = true
        };
        
        // Assert
        chunk.ChunkType.Should().Be("error");
        chunk.IsRetryable.Should().BeTrue();
        chunk.ErrorCode.Should().Be("RATE_LIMIT");
    }
}
```

## 체크리스트

- [ ] StreamChunk 기본 클래스 구현
- [ ] 모든 청크 타입 구현 (Token, ToolCall, Status, Final, Error)
- [ ] IStreamAggregator 인터페이스 정의
- [ ] StreamingExtensions 유틸리티 구현
- [ ] 단위 테스트 작성
- [ ] 문서화 완료

## 다음 단계
Phase 4: Step & Plan 구조