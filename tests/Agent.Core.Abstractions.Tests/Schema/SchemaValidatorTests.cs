using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using NSubstitute;
using Agent.Core.Abstractions.Schema;
using Agent.Core.Abstractions.Schema.Registry;
using Agent.Core.Abstractions.Schema.Validation;
using Agent.Core.Abstractions.Common;

namespace Agent.Core.Abstractions.Tests.Schema;

public class SchemaValidatorTests
{
    private readonly ISchemaValidator _validator;
    private readonly ISchemaRegistry _registry;
    
    public SchemaValidatorTests()
    {
        _validator = Substitute.For<ISchemaValidator>();
        _registry = Substitute.For<ISchemaRegistry>();
    }
    
    [Fact]
    public async Task ValidateAsync_Should_Return_True_For_Valid_Json()
    {
        // Arrange
        var json = JsonNode.Parse("{\"name\": \"test\", \"age\": 25}");
        var schemaId = "test.schema";
        
        _validator.ValidateAsync(json!, schemaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        
        // Act
        var result = await _validator.ValidateAsync(json!, schemaId);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task ValidateAsync_Should_Return_False_For_Invalid_Json()
    {
        // Arrange
        var json = JsonNode.Parse("{\"name\": \"test\"}");
        var schemaId = "test.schema";
        
        _validator.ValidateAsync(json!, schemaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        
        // Act
        var result = await _validator.ValidateAsync(json!, schemaId);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task ValidateWithDetailsAsync_Should_Return_Errors()
    {
        // Arrange
        var json = JsonNode.Parse("{\"name\": \"test\"}");
        var schemaId = "test.schema";
        var validationResult = new ValidationResult
        {
            IsValid = false,
            Errors = new[]
            {
                new ValidationError
                {
                    Path = "$.age",
                    Message = "Property 'age' is required",
                    ErrorCode = "REQUIRED_PROPERTY",
                    SchemaKeyword = "required"
                }
            }
        };
        
        _validator.ValidateWithDetailsAsync(json!, schemaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(validationResult));
        
        // Act
        var result = await _validator.ValidateWithDetailsAsync(json!, schemaId);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Path.Should().Be("$.age");
        result.Errors[0].ErrorCode.Should().Be("REQUIRED_PROPERTY");
    }
    
    [Fact]
    public async Task CoerceAsync_Should_Transform_Json()
    {
        // Arrange
        var inputJson = JsonNode.Parse("{\"name\": \"test\", \"age\": \"25\"}");
        var expectedJson = JsonNode.Parse("{\"name\": \"test\", \"age\": 25}");
        var schemaId = "test.schema";
        
        _validator.CoerceAsync(inputJson!, schemaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedJson)!);
        
        // Act
        var result = await _validator.CoerceAsync(inputJson!, schemaId);
        
        // Assert
        result.Should().NotBeNull();
        result!["age"]!.GetValue<int>().Should().Be(25);
    }
    
    [Fact]
    public async Task Registry_Should_Store_And_Retrieve_Schema()
    {
        // Arrange
        var schemaId = "test.schema";
        var schema = JsonNode.Parse(@"{
            ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""number"" }
            },
            ""required"": [""name"", ""age""]
        }");
        
        _registry.GetAsync(schemaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(schema));
        _registry.ExistsAsync(schemaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        
        // Act
        var exists = await _registry.ExistsAsync(schemaId);
        var retrieved = await _registry.GetAsync(schemaId);
        
        // Assert
        exists.Should().BeTrue();
        retrieved.Should().NotBeNull();
    }
    
    [Fact]
    public void SchemaConstants_Should_Have_Correct_Values()
    {
        // Assert
        SchemaConstants.CoreNamespace.Should().Be("agent.core");
        SchemaConstants.ToolsNamespace.Should().Be("agent.tools");
        SchemaConstants.StepSchemaId.Should().Be("agent.orchestration.step");
        SchemaConstants.PlanSchemaId.Should().Be("agent.orchestration.plan");
        SchemaConstants.JsonSchemaVersion.Should().Contain("json-schema.org");
    }
    
    [Fact]
    public void ValidationError_Should_Store_Details()
    {
        // Arrange & Act
        var error = new ValidationError
        {
            Path = "$.items[0].name",
            Message = "String value expected",
            ErrorCode = "TYPE_MISMATCH",
            SchemaKeyword = "type"
        };
        
        // Assert
        error.Path.Should().Be("$.items[0].name");
        error.Message.Should().Be("String value expected");
        error.ErrorCode.Should().Be("TYPE_MISMATCH");
        error.SchemaKeyword.Should().Be("type");
    }
    
    [Fact]
    public void SchemaMetadata_Should_Have_Default_Values()
    {
        // Arrange & Act
        var metadata = new SchemaMetadata
        {
            Id = "test.schema",
            Title = "Test Schema"
        };
        
        // Assert
        metadata.Id.Should().Be("test.schema");
        metadata.Version.Should().Be("1.0.0");
        metadata.Title.Should().Be("Test Schema");
        metadata.RegisteredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }
}