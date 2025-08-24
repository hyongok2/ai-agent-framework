using System;

namespace Agent.Core.Abstractions.Common.Identifiers;

/// <summary>
/// 실행 내 각 단계를 식별하는 ID
/// </summary>
public readonly record struct StepId
{
    public string Value { get; }
    
    public StepId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("StepId cannot be empty", nameof(value));
        Value = value;
    }
    
    /// <summary>
    /// 순서 기반 StepId 생성
    /// </summary>
    public static StepId New(int sequence) => new($"step_{sequence:D4}");
    
    /// <summary>
    /// 부모-자식 관계가 있는 StepId 생성
    /// </summary>
    public static StepId NewChild(StepId parent, int childIndex) 
        => new($"{parent.Value}_{childIndex:D2}");
    
    public override string ToString() => Value;
    
    public static implicit operator string(StepId id) => id.Value;
    public static explicit operator StepId(string value) => new(value);
}