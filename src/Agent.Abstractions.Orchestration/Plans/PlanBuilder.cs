using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Agent.Abstractions.Core.Common.Identifiers;
using Agent.Abstractions.Orchestration.Configuration;
using Agent.Abstractions.Orchestration.Core;
using Agent.Abstractions.Orchestration.Execution;

namespace Agent.Abstractions.Orchestration.Plans;

/// <summary>
/// Plan을 구성하는 빌더
/// </summary>
public class PlanBuilder : IPlanBuilder
{
    private readonly List<ExecutionStep> _steps = new();
    private string _id = Guid.NewGuid().ToString();
    private OrchestrationType _type = OrchestrationType.Fixed;
    private string? _name;
    private string? _description;
    private readonly Dictionary<string, object> _context = new();
    private PlanSettings _settings = new();
    
    public IPlanBuilder WithId(string id)
    {
        _id = id;
        return this;
    }
    
    public IPlanBuilder WithType(OrchestrationType type)
    {
        _type = type;
        return this;
    }
    
    public IPlanBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public IPlanBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }
    
    public IPlanBuilder AddStep(ExecutionStep executionStep)
    {
        _steps.Add(executionStep);
        return this;
    }
    
    public IPlanBuilder AddLlmStep(string prompt, StepId? dependsOn = null)
    {
        var step = new ExecutionStep
        {
            Id = StepId.New(_steps.Count + 1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse($"{{\"prompt\": \"{prompt}\"}}"),
            Dependencies = dependsOn != null ? new[] { dependsOn.Value } : Array.Empty<StepId>()
        };
        
        return AddStep(step);
    }
    
    public IPlanBuilder AddToolStep(string toolName, JsonDocument arguments, StepId? dependsOn = null)
    {
        var step = new ExecutionStep
        {
            Id = StepId.New(_steps.Count + 1),
            Kind = StepKind.ToolCall,
            Input = JsonDocument.Parse($"{{\"tool\": \"{toolName}\", \"arguments\": {arguments.RootElement}}}"),
            Dependencies = dependsOn != null ? new[] { dependsOn.Value } : Array.Empty<StepId>()
        };
        
        return AddStep(step);
    }
    
    public IPlanBuilder AddParallelSteps(params Action<IPlanBuilder>[] stepBuilders)
    {
        var parallelSteps = new List<ExecutionStep>();
        
        foreach (var builder in stepBuilders)
        {
            var subBuilder = new PlanBuilder();
            builder(subBuilder);
            parallelSteps.AddRange(subBuilder._steps);
        }
        
        var parallelStep = new ExecutionStep
        {
            Id = StepId.New(_steps.Count + 1),
            Kind = StepKind.Parallel,
            Input = JsonDocument.Parse($"{{\"steps\": {JsonSerializer.Serialize(parallelSteps)}}}")
        };
        
        return AddStep(parallelStep);
    }
    
    public IPlanBuilder WithContext(string key, object value)
    {
        _context[key] = value;
        return this;
    }
    
    public IPlanBuilder WithSettings(Action<PlanSettings> configure)
    {
        var newSettings = _settings with { };
        configure(newSettings);
        _settings = newSettings;
        return this;
    }
    
    public Plan Build()
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException("Plan must have at least one step");
        
        var plan = new Plan
        {
            Id = _id,
            Type = _type,
            Steps = _steps.ToArray(),
            Name = _name,
            Description = _description,
            Context = _context,
            Settings = _settings
        };
        
        if (!plan.ValidateExecutionOrder())
            throw new InvalidOperationException("Invalid execution order: some dependencies are not defined");
        
        return plan;
    }
    
}