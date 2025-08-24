using System;
using Xunit;
using FluentAssertions;
using Agent.Core.Abstractions.Common;
using Agent.Core.Abstractions.Common.Identifiers;

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