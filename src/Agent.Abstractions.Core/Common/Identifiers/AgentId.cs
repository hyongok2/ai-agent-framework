namespace Agent.Abstractions.Core.Common.Identifiers;

/// <summary>
/// 에이전트를 고유하게 식별하는 ID
/// </summary>
public readonly record struct AgentId
{
    public string Value { get; }
    
    public AgentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AgentId cannot be empty", nameof(value));
        Value = value;
    }
    
    /// <summary>
    /// 새로운 AgentId 생성
    /// </summary>
    public static AgentId New() => new($"agent_{Guid.NewGuid():N}");
    
    /// <summary>
    /// 명명된 AgentId 생성
    /// </summary>
    public static AgentId FromName(string name) 
        => new($"agent_{name}_{DateTime.UtcNow:yyyyMMddHHmmss}");
    
    public override string ToString() => Value;
    
    public static implicit operator string(AgentId id) => id.Value;
    public static explicit operator AgentId(string value) => new(value);
}