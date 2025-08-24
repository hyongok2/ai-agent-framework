using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using NSubstitute;
using Agent.Core.Abstractions.Common;
using Agent.Core.Abstractions.Llm.Core;
using Agent.Core.Abstractions.Llm.Models.Completion;
using Agent.Core.Abstractions.Llm.Models.Embeddings;
using Agent.Core.Abstractions.Llm.Models.Functions;
using Agent.Core.Abstractions.Llm.Models.Streaming;
using Agent.Core.Abstractions.Llm.Registry;
using Agent.Core.Abstractions.Llm.Enums;

namespace Agent.Core.Abstractions.Tests.Llm;

public class LlmTests
{
    private readonly ILlmClient _client;
    private readonly ILlmRegistry _registry;

    public LlmTests()
    {
        _client = Substitute.For<ILlmClient>();
        _registry = Substitute.For<ILlmRegistry>();
    }

    [Fact]
    public async Task LlmClient_Should_Send_Request_Successfully()
    {
        // Arrange
        var request = new LlmRequest
        {
            Model = "test-model",
            Messages = new[]
            {
                new LlmMessage
                {
                    Role = MessageRole.User,
                    Content = "Hello, world!"
                }
            }
        };

        var expectedResponse = new LlmResponse
        {
            RequestId = request.RequestId,
            Model = request.Model,
            Choices = new[]
            {
                new LlmChoice
                {
                    Index = 0,
                    Message = new LlmMessage
                    {
                        Role = MessageRole.Assistant,
                        Content = "Hello! How can I help you?"
                    },
                    FinishReason = FinishReason.Stop
                }
            }
        };

        _client.SendAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.RequestId.Should().Be(request.RequestId);
        response.Model.Should().Be(request.Model);
        response.Choices.Should().HaveCount(1);
    }

    [Fact]
    public void LlmClient_Should_Have_Basic_Properties()
    {
        // Arrange & Act
        _client.ProviderName.Returns("test-provider");
        _client.Status.Returns(ClientStatus.Healthy);
        _client.SupportedModels.Returns(new[] { "test-model-1", "test-model-2" });

        // Assert
        _client.ProviderName.Should().Be("test-provider");
        _client.Status.Should().Be(ClientStatus.Healthy);
        _client.SupportedModels.Should().HaveCount(2);
    }

    [Fact]
    public async Task LlmRegistry_Should_Register_And_Retrieve_Client()
    {
        // Arrange
        var providerName = "test-provider";
        
        _registry.GetAsync(providerName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ILlmClient?>(_client));

        // Act
        var retrieved = await _registry.GetAsync(providerName);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().Be(_client);
    }

    [Fact]
    public void LlmRequest_Should_Have_Valid_Properties()
    {
        // Arrange & Act
        var request = new LlmRequest
        {
            Model = "gpt-4",
            Messages = new[]
            {
                new LlmMessage
                {
                    Role = MessageRole.System,
                    Content = "You are a helpful assistant."
                },
                new LlmMessage
                {
                    Role = MessageRole.User,
                    Content = "Hello!"
                }
            }
        };

        // Assert
        request.Model.Should().Be("gpt-4");
        request.Messages.Should().HaveCount(2);
        request.Messages[0].Role.Should().Be(MessageRole.System);
        request.Messages[1].Role.Should().Be(MessageRole.User);
        request.RequestId.Should().NotBeEmpty();
    }

    [Fact]
    public void MessageRole_Enum_Should_Have_Correct_Values()
    {
        // Assert
        Enum.GetNames<MessageRole>().Should().Contain(new[]
        {
            nameof(MessageRole.System),
            nameof(MessageRole.User),
            nameof(MessageRole.Assistant),
            nameof(MessageRole.Function),
            nameof(MessageRole.Tool)
        });
    }

    [Fact]
    public void ClientStatus_Enum_Should_Have_Correct_Values()
    {
        // Assert
        Enum.GetNames<ClientStatus>().Should().Contain(new[]
        {
            nameof(ClientStatus.Healthy),
            nameof(ClientStatus.Warning),
            nameof(ClientStatus.Error),
            nameof(ClientStatus.Unavailable)
        });
    }

    [Fact]
    public void FinishReason_Enum_Should_Have_Correct_Values()
    {
        // Assert
        Enum.GetNames<FinishReason>().Should().Contain(new[]
        {
            nameof(FinishReason.Stop),
            nameof(FinishReason.Length),
            nameof(FinishReason.ContentFilter)
        });
    }

    [Fact]
    public void LlmProvider_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var provider = new LlmProvider
        {
            Name = "openai",
            DisplayName = "OpenAI",
            Description = "OpenAI GPT models"
        };

        // Assert
        provider.Name.Should().Be("openai");
        provider.DisplayName.Should().Be("OpenAI");
        provider.Description.Should().Be("OpenAI GPT models");
        provider.IsEnabled.Should().BeTrue(); // Default value
        provider.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }
}