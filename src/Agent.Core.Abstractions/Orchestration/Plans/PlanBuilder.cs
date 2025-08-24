using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Agent.Core.Abstractions.Common.Identifiers;
using Agent.Core.Abstractions.Orchestration.Configuration;
using Agent.Core.Abstractions.Orchestration.Execution;

namespace Agent.Core.Abstractions.Orchestration.Plans;

/// <summary>
/// Plan을 구성하는 빌더
/// </summary>
public class PlanBuilder
{
    private readonly List<ExecutionStep> _steps = new();
    private string _id = Guid.NewGuid().ToString();
    private OrchestrationType _type = OrchestrationType.Fixed;
    private string? _name;
    private string? _description;
    private readonly Dictionary<string, object> _context = new();
    private PlanSettings _settings = new();
    
    public PlanBuilder WithId(string id)
    {
        _id = id;
        return this;
    }
    
    public PlanBuilder WithType(OrchestrationType type)
    {
        _type = type;
        return this;
    }
    
    public PlanBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public PlanBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }
    
    public PlanBuilder AddStep(ExecutionStep step)
    {
        _steps.Add(step);
        return this;
    }
    
    public PlanBuilder AddLlmStep(string prompt, StepId? dependsOn = null)
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
    
    public PlanBuilder AddToolStep(string toolName, JsonDocument arguments, StepId? dependsOn = null)
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
    
    public PlanBuilder AddParallelSteps(params Action<PlanBuilder>[] stepBuilders)
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
    
    public PlanBuilder WithContext(string key, object value)
    {
        _context[key] = value;
        return this;
    }
    
    public PlanBuilder WithSettings(Action<PlanSettings> configure)
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