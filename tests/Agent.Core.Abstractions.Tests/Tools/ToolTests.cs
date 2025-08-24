using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using NSubstitute;
using Agent.Core.Abstractions.Common;
using Agent.Core.Abstractions.Common.Identifiers;
using Agent.Core.Abstractions.Tools.Core;
using Agent.Core.Abstractions.Tools.Registry;
using Agent.Core.Abstractions.Tools.Execution;
using Agent.Core.Abstractions.Tools.Metadata;

namespace Agent.Core.Abstractions.Tests.Tools;

public class ToolTests
{
    private readonly ITool _tool;
    private readonly IToolRegistry _registry;
    private readonly ToolId _toolId;
    
    public ToolTests()
    {
        _tool = Substitute.For<ITool>();
        _registry = Substitute.For<IToolRegistry>();
        _toolId = new ToolId("internal", "math", "calculator", "1.0.0");
    }
    
    [Fact]
    public async Task Tool_Should_Execute_Successfully()
    {
        // Arrange
        var input = JsonNode.Parse("{\"operation\": \"add\", \"a\": 5, \"b\": 3}")!;
        var output = JsonNode.Parse("{\"result\": 8}")!;
        var context = new ToolContext
        {
            RunId = RunId.New(),
            StepId = StepId.New(1)
        };
        
        var expectedResult = ToolResult.CreateSuccess(output);
        
        _tool.ExecuteAsync(input!, context, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));
        
        // Act
        var result = await _tool.ExecuteAsync(input!, context);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().NotBeNull();
        result.Error.Should().BeNull();
    }
    
    [Fact]
    public async Task Tool_Should_Return_Failure_On_Error()
    {
        // Arrange
        var input = JsonNode.Parse("{\"invalid\": \"input\"}")!;
        var context = new ToolContext
        {
            RunId = RunId.New(),
            StepId = StepId.New(1)
        };
        
        var expectedResult = ToolResult.CreateFailure("Invalid input format", "INVALID_INPUT");
        
        _tool.ExecuteAsync(input!, context, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));
        
        // Act
        var result = await _tool.ExecuteAsync(input!, context);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid input format");
        result.ErrorCode.Should().Be("INVALID_INPUT");
    }
    
    [Fact]
    public void ToolDescriptor_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var descriptor = new ToolDescriptor
        {
            Id = _toolId,
            Name = "Calculator",
            Description = "Basic math operations",
            InputSchema = JsonNode.Parse(@"{""type"": ""object"", ""properties"": {""operation"": {""type"": ""string""}}}")!,
            OutputSchema = JsonNode.Parse(@"{""type"": ""object"", ""properties"": {""result"": {""type"": ""number""}}}")!,
            Category = "Math",
            Tags = new[] { "math", "calculation" },
            Capabilities = new ToolCapabilities
            {
                SupportsStreaming = false,
                IsRetryable = true,
                MaxExecutionTime = TimeSpan.FromSeconds(30)
            }
        };
        
        // Assert
        descriptor.Id.Should().Be(_toolId);
        descriptor.Name.Should().Be("Calculator");
        descriptor.Category.Should().Be("Math");
        descriptor.Tags.Should().Contain("math");
        descriptor.Capabilities.IsRetryable.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToolRegistry_Should_Register_And_Retrieve_Tools()
    {
        // Arrange
        var descriptor = new ToolDescriptor
        {
            Id = _toolId,
            Name = "Calculator",
            InputSchema = JsonNode.Parse("{}")!,
            OutputSchema = JsonNode.Parse("{}")!
        };
        
        _tool.Describe().Returns(descriptor);
        _registry.GetAsync(_toolId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ITool?>(_tool));
        _registry.ExistsAsync(_toolId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        
        // Act
        var exists = await _registry.ExistsAsync(_toolId);
        var retrieved = await _registry.GetAsync(_toolId);
        
        // Assert
        exists.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.Describe().Name.Should().Be("Calculator");
    }
    
    [Fact]
    public async Task ToolRegistry_Should_Search_Tools_By_Criteria()
    {
        // Arrange
        var criteria = new ToolSearchCriteria
        {
            Category = "Math",
            RequiredTags = new[] { "calculation" },
            CapabilityRequirements = new ToolCapabilityRequirements
            {
                RequiresRetryable = true,
                MaxAllowedExecutionTime = TimeSpan.FromMinutes(1)
            }
        };
        
        var descriptors = new[]
        {
            new ToolDescriptor
            {
                Id = _toolId,
                Name = "Calculator",
                InputSchema = JsonNode.Parse("{}")!,
                OutputSchema = JsonNode.Parse("{}")!,
                Category = "Math",
                Tags = new[] { "calculation" }
            }
        };
        
        _registry.SearchAsync(criteria, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptors));
        
        // Act
        var results = await _registry.SearchAsync(criteria);
        
        // Assert
        results.Should().HaveCount(1);
        results[0].Category.Should().Be("Math");
        results[0].Tags.Should().Contain("calculation");
    }
    
    [Fact]
    public void ToolResult_Factory_Methods_Should_Work()
    {
        // Arrange & Act
        var successResult = ToolResult.CreateSuccess(JsonNode.Parse("{\"value\": 42}")!);
        var failureResult = ToolResult.CreateFailure("Something went wrong", "ERROR_CODE");
        
        // Assert
        successResult.Success.Should().BeTrue();
        successResult.Output.Should().NotBeNull();
        successResult.Error.Should().BeNull();
        
        failureResult.Success.Should().BeFalse();
        failureResult.Output.Should().BeNull();
        failureResult.Error.Should().Be("Something went wrong");
        failureResult.ErrorCode.Should().Be("ERROR_CODE");
    }
    
    [Fact]
    public void ToolContext_Should_Have_Required_Information()
    {
        // Arrange & Act
        var context = new ToolContext
        {
            RunId = RunId.New(),
            StepId = StepId.New(1),
            UserId = "user123",
            SessionId = "session456",
            Environment = new ExecutionEnvironment
            {
                Type = "production",
                Region = "us-east-1",
                Language = "en"
            },
            TraceId = "trace789"
        };
        
        // Assert
        context.RunId.Should().NotBe(default);
        context.StepId.Should().NotBe(default);
        context.UserId.Should().Be("user123");
        context.Environment.Type.Should().Be("production");
        context.TraceId.Should().Be("trace789");
    }
    
    [Fact]
    public void ToolExecutionMetrics_Should_Calculate_Duration()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime.AddSeconds(5);
        
        // Act
        var metrics = new ToolExecutionMetrics
        {
            StartTime = startTime,
            EndTime = endTime,
            InputSize = 1024,
            OutputSize = 512,
            ApiCalls = 2,
            Cost = 0.001m
        };
        
        // Assert
        metrics.Duration.Should().Be(TimeSpan.FromSeconds(5));
        metrics.InputSize.Should().Be(1024);
        metrics.ApiCalls.Should().Be(2);
        metrics.Cost.Should().Be(0.001m);
    }
    
    [Fact]
    public void ToolCapabilities_Should_Have_Default_Values()
    {
        // Arrange & Act
        var capabilities = new ToolCapabilities();
        
        // Assert
        capabilities.SupportsStreaming.Should().BeFalse();
        capabilities.SupportsParallelExecution.Should().BeTrue();
        capabilities.IsRetryable.Should().BeTrue();
        capabilities.IsIdempotent.Should().BeTrue();
        capabilities.IsCacheable.Should().BeTrue();
    }
}