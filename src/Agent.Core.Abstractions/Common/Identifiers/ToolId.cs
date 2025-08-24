using System;

namespace Agent.Core.Abstractions.Common.Identifiers;

/// <summary>
/// 도구 식별자
/// </summary>
public readonly record struct ToolId
{
    public string Provider { get; }
    public string Namespace { get; }
    public string Name { get; }
    public string Version { get; }
    
    public ToolId(string provider, string @namespace, string name, string version)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty", nameof(provider));
        if (string.IsNullOrWhiteSpace(@namespace))
            throw new ArgumentException("Namespace cannot be empty", nameof(@namespace));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be empty", nameof(version));
            
        Provider = provider;
        Namespace = @namespace;
        Name = name;
        Version = version;
    }
    
    public string FullName => $"{Provider}/{Namespace}/{Name}/{Version}";
    
    public override string ToString() => FullName;
    
    public static ToolId Parse(string fullName)
    {
        var parts = fullName.Split('/');
        if (parts.Length != 4)
            throw new FormatException($"Invalid tool ID format: {fullName}");
            
        return new ToolId(parts[0], parts[1], parts[2], parts[3]);
    }
}