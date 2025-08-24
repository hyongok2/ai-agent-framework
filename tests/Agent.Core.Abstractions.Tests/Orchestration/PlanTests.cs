using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Agent.Core.Abstractions.Common;
using Agent.Core.Abstractions.Common.Identifiers;
using Agent.Core.Abstractions.Orchestration.Plans;
using Agent.Core.Abstractions.Orchestration.Execution;
using Agent.Core.Abstractions.Orchestration.Configuration;

namespace Agent.Core.Abstractions.Tests.Orchestration;

public class PlanTests
{
    [Fact]
    public void Plan_Should_Validate_Execution_Order()
    {
        // Arrange
        var step1 = new ExecutionStep
        {
            Id = StepId.New(1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}")
        };
        
        var step2 = new ExecutionStep
        {
            Id = StepId.New(2),
            Kind = StepKind.ToolCall,
            Input = JsonDocument.Parse("{}"),
            Dependencies = new[] { StepId.New(1) }
        };
        
        var plan = new Plan
        {
            Id = "test-plan",
            Type = OrchestrationType.Fixed,
            Steps = new[] { step1, step2 }
        };
        
        // Act
        var isValid = plan.ValidateExecutionOrder();
        
        // Assert
        isValid.Should().BeTrue();
    }
    
    [Fact]
    public void Plan_Should_Detect_Invalid_Dependencies()
    {
        // Arrange
        var step1 = new ExecutionStep
        {
            Id = StepId.New(1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}"),
            Dependencies = new[] { StepId.New(99) } // Non-existent dependency
        };
        
        var plan = new Plan
        {
            Id = "test-plan",
            Type = OrchestrationType.Fixed,
            Steps = new[] { step1 }
        };
        
        // Act
        var isValid = plan.ValidateExecutionOrder();
        
        // Assert
        isValid.Should().BeFalse();
    }
    
    [Fact]
    public void GetExecutableSteps_Should_Return_Ready_Steps()
    {
        // Arrange
        var step1 = new ExecutionStep
        {
            Id = StepId.New(1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}")
        };
        
        var step2 = new ExecutionStep
        {
            Id = StepId.New(2),
            Kind = StepKind.ToolCall,
            Input = JsonDocument.Parse("{}"),
            Dependencies = new[] { StepId.New(1) }
        };
        
        var step3 = new ExecutionStep
        {
            Id = StepId.New(3),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}")
        };
        
        var plan = new Plan
        {
            Id = "test-plan",
            Type = OrchestrationType.Fixed,
            Steps = new[] { step1, step2, step3 }
        };
        
        // Act
        var executableSteps = plan.GetExecutableSteps(new HashSet<StepId>()).ToList();
        
        // Assert
        executableSteps.Should().HaveCount(2); // step1 and step3
        executableSteps.Should().Contain(s => s.Id == step1.Id);
        executableSteps.Should().Contain(s => s.Id == step3.Id);
    }
    
    [Fact]
    public void PlanBuilder_Should_Create_Valid_Plan()
    {
        // Act
        var plan = new PlanBuilder()
            .WithId("builder-plan")
            .WithName("Test Plan")
            .WithType(OrchestrationType.Planner)
            .AddLlmStep("Generate summary")
            .AddToolStep("calculator", JsonDocument.Parse("{\"operation\": \"add\"}"))
            .WithContext("user", "test-user")
            .Build();
        
        // Assert
        plan.Id.Should().Be("builder-plan");
        plan.Name.Should().Be("Test Plan");
        plan.Type.Should().Be(OrchestrationType.Planner);
        plan.Steps.Should().HaveCount(2);
        plan.Context["user"].Should().Be("test-user");
    }
    
    [Fact]
    public void PlanBuilder_Should_Throw_On_Empty_Steps()
    {
        // Act
        var act = () => new PlanBuilder()
            .WithId("empty-plan")
            .Build();
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have at least one step*");
    }
    
    [Fact]
    public void PlanBuilder_Should_Throw_On_Invalid_Dependencies()
    {
        // Arrange
        var invalidStep = new ExecutionStep
        {
            Id = StepId.New(1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}"),
            Dependencies = new[] { StepId.New(99) } // Invalid dependency
        };
        
        // Act
        var act = () => new PlanBuilder()
            .WithId("invalid-plan")
            .AddStep(invalidStep)
            .Build();
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid execution order*");
    }
    
    [Fact]
    public void Step_Should_Have_Default_Values()
    {
        // Arrange & Act
        var step = new ExecutionStep
        {
            Id = StepId.New(1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}")
        };
        
        // Assert
        step.Status.Should().Be(StepStatus.Pending);
        step.Dependencies.Should().BeEmpty();
        step.Metadata.Should().BeEmpty();
    }
    
    [Fact]
    public void RetryPolicy_Should_Have_Default_Values()
    {
        // Arrange & Act
        var policy = new RetryPolicy();
        
        // Assert
        policy.MaxAttempts.Should().Be(3);
        policy.Strategy.Should().Be(RetryStrategy.ExponentialBackoff);
        policy.InitialDelay.Should().Be(TimeSpan.FromSeconds(1));
        policy.MaxDelay.Should().Be(TimeSpan.FromMinutes(1));
        policy.RetryableErrors.Should().BeEmpty();
    }
    
    [Fact]
    public void PlanSettings_Should_Have_Default_Values()
    {
        // Arrange & Act
        var settings = new PlanSettings();
        
        // Assert
        settings.MaxParallelSteps.Should().Be(5);
        settings.StopOnFirstFailure.Should().BeTrue();
        settings.LogLevel.Should().Be(LogLevel.Information);
    }
}