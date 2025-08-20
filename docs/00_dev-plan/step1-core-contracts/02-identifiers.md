# Phase 2: 핵심 식별자 구현

## 목표
실행 단위와 단계를 식별하는 기본 타입 구현

## 구현 파일

### 1. Common/Identifiers.cs

```csharp
using System;

namespace Agent.Core.Abstractions.Common;

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

/// <summary>
/// 도구 식별자
/// </summary>
public readonly record struct ToolId
{
    public string Provider { get; }
    public string Namespace { get; }
    public string Name { get; }
    public string Version { get; }
    
    public ToolId(string provider, string ns, string name, string version)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty", nameof(provider));
        if (string.IsNullOrWhiteSpace(ns))
            throw new ArgumentException("Namespace cannot be empty", nameof(ns));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be empty", nameof(version));
            
        Provider = provider;
        Namespace = ns;
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
```

### 2. Common/Exceptions.cs

```csharp
using System;

namespace Agent.Core.Abstractions.Common;

/// <summary>
/// Agent Framework 기본 예외
/// </summary>
public class AgentException : Exception
{
    public string? ErrorCode { get; }
    
    public AgentException(string message, string? errorCode = null) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public AgentException(string message, Exception innerException, string? errorCode = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// 스키마 검증 실패 예외
/// </summary>
public class SchemaValidationException : AgentException
{
    public string SchemaId { get; }
    public string[] Errors { get; }
    
    public SchemaValidationException(string schemaId, string[] errors)
        : base($"Schema validation failed for {schemaId}: {string.Join(", ", errors)}", "SCHEMA_VALIDATION_FAILED")
    {
        SchemaId = schemaId;
        Errors = errors;
    }
}

/// <summary>
/// 도구 실행 실패 예외
/// </summary>
public class ToolExecutionException : AgentException
{
    public string ToolName { get; }
    
    public ToolExecutionException(string toolName, string message, Exception? innerException = null)
        : base($"Tool execution failed for {toolName}: {message}", innerException, "TOOL_EXECUTION_FAILED")
    {
        ToolName = toolName;
    }
}
```

### 3. Tests/Common/IdentifiersTests.cs

```csharp
using System;
using Xunit;
using FluentAssertions;
using Agent.Core.Abstractions.Common;

namespace Agent.Core.Abstractions.Tests.Common;

public class RunIdTests
{
    [Fact]
    public void New_Should_Generate_Unique_Ids()
    {
        // Arrange & Act
        var id1 = RunId.New();
        var id2 = RunId.New();
        
        // Assert
        id1.Should().NotBe(id2);
        id1.Value.Should().StartWith("run_");
        id2.Value.Should().StartWith("run_");
    }
    
    [Fact]
    public void NewWithTimestamp_Should_Include_Timestamp()
    {
        // Arrange & Act
        var id = RunId.NewWithTimestamp();
        
        // Assert
        id.Value.Should().StartWith("run_");
        id.Value.Should().MatchRegex(@"^run_\d{14}_");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_Should_Throw_On_Invalid_Value(string value)
    {
        // Act
        var act = () => new RunId(value);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }
    
    [Fact]
    public void Implicit_Conversion_To_String_Should_Work()
    {
        // Arrange
        var id = RunId.New();
        
        // Act
        string value = id;
        
        // Assert
        value.Should().Be(id.Value);
    }
    
    [Fact]
    public void Explicit_Conversion_From_String_Should_Work()
    {
        // Arrange
        var value = "run_test123";
        
        // Act
        var id = (RunId)value;
        
        // Assert
        id.Value.Should().Be(value);
    }
}

public class StepIdTests
{
    [Fact]
    public void New_Should_Generate_Sequential_Id()
    {
        // Act
        var id1 = StepId.New(1);
        var id2 = StepId.New(42);
        var id3 = StepId.New(999);
        
        // Assert
        id1.Value.Should().Be("step_0001");
        id2.Value.Should().Be("step_0042");
        id3.Value.Should().Be("step_0999");
    }
    
    [Fact]
    public void NewChild_Should_Create_Hierarchical_Id()
    {
        // Arrange
        var parent = StepId.New(1);
        
        // Act
        var child1 = StepId.NewChild(parent, 1);
        var child2 = StepId.NewChild(parent, 2);
        
        // Assert
        child1.Value.Should().Be("step_0001_01");
        child2.Value.Should().Be("step_0001_02");
    }
}

public class ToolIdTests
{
    [Fact]
    public void Constructor_Should_Create_Valid_ToolId()
    {
        // Act
        var toolId = new ToolId("internal", "math", "calculator", "1.0.0");
        
        // Assert
        toolId.Provider.Should().Be("internal");
        toolId.Namespace.Should().Be("math");
        toolId.Name.Should().Be("calculator");
        toolId.Version.Should().Be("1.0.0");
        toolId.FullName.Should().Be("internal/math/calculator/1.0.0");
    }
    
    [Theory]
    [InlineData("", "ns", "name", "ver")]
    [InlineData("prov", "", "name", "ver")]
    [InlineData("prov", "ns", "", "ver")]
    [InlineData("prov", "ns", "name", "")]
    public void Constructor_Should_Throw_On_Empty_Parts(string provider, string ns, string name, string version)
    {
        // Act
        var act = () => new ToolId(provider, ns, name, version);
        
        // Assert
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void Parse_Should_Parse_Valid_FullName()
    {
        // Arrange
        var fullName = "plugin/http/client/2.0.0";
        
        // Act
        var toolId = ToolId.Parse(fullName);
        
        // Assert
        toolId.Provider.Should().Be("plugin");
        toolId.Namespace.Should().Be("http");
        toolId.Name.Should().Be("client");
        toolId.Version.Should().Be("2.0.0");
    }
    
    [Theory]
    [InlineData("invalid")]
    [InlineData("only/two")]
    [InlineData("one/two/three")]
    [InlineData("one/two/three/four/five")]
    public void Parse_Should_Throw_On_Invalid_Format(string fullName)
    {
        // Act
        var act = () => ToolId.Parse(fullName);
        
        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage($"*Invalid tool ID format: {fullName}*");
    }
}
```

## 체크리스트

- [ ] RunId 구현 완료
- [ ] StepId 구현 완료
- [ ] ToolId 구현 완료
- [ ] 예외 타입 정의
- [ ] 단위 테스트 작성
- [ ] 테스트 커버리지 확인
- [ ] XML 문서 주석 완료

## 다음 단계
Phase 3: 스트리밍 이벤트 구조