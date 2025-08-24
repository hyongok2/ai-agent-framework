using System;

namespace Agent.Core.Abstractions.Common.Identifiers;

/// <summary>
/// 실행 단위를 고유하게 식별하는 ID
/// </summary>
public readonly record struct RunId
{
    public string Value { get; }
    
    public RunId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("RunId cannot be empty", nameof(value));
        Value = value;
    }
    
    /// <summary>
    /// 새로운 RunId 생성
    /// </summary>
    public static RunId New() => new($"run_{Guid.NewGuid():N}");
    
    /// <summary>
    /// 타임스탬프 기반 RunId 생성
    /// </summary>
    public static RunId NewWithTimestamp() 
        => new($"run_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 32));
    
    public override string ToString() => Value;
    
    public static implicit operator string(RunId id) => id.Value;
    public static explicit operator RunId(string value) => new(value);
}