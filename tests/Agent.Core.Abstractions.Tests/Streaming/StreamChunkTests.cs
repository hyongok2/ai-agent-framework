using System;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Agent.Core.Abstractions.Common;
using Agent.Core.Abstractions.Common.Identifiers;
using Agent.Core.Abstractions.Streaming.Chunks;
using Agent.Core.Abstractions.Streaming.Metrics;

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