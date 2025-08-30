using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AIAgentFramework.LLM.Factories;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.Core.Interfaces;
using System.Collections.Generic;

namespace AIAgentFramework.Tests;

/// <summary>
/// LLM Provider 통합 테스트
/// </summary>
[TestFixture]
public class LLMProviderIntegrationTests
{
    private IConfiguration _configuration;
    private IHttpClientFactory _httpClientFactory;
    private ILoggerFactory _loggerFactory;
    private LLMProviderFactory _factory;

    [SetUp]
    public void Setup()
    {
        // 테스트용 설정 구성
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LLM:DefaultProvider"] = "openai",
            ["LLM:OpenAI:ApiKey"] = "test-openai-key",
            ["LLM:OpenAI:BaseUrl"] = "https://api.openai.com/v1",
            ["LLM:Claude:ApiKey"] = "test-claude-key",
            ["LLM:Claude:BaseUrl"] = "https://api.anthropic.com/v1",
            ["LLM:Local:BaseUrl"] = "http://localhost:11434",
            ["LLM:Local:DefaultModel"] = "llama2"
        });
        _configuration = configBuilder.Build();

        _httpClientFactory = new TestHttpClientFactory();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _factory = new LLMProviderFactory(_configuration, _httpClientFactory, _loggerFactory);
    }

    [Test]
    public void CreateProvider_OpenAI_ShouldReturnOpenAIProvider()
    {
        // Act
        var provider = _factory.CreateProvider("openai");

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<OpenAIProvider>());
        Assert.That(provider.Name, Is.EqualTo("OpenAI"));
        Assert.That(provider.SupportedModels, Contains.Item("gpt-4"));
        Assert.That(provider.DefaultModel, Is.EqualTo("gpt-4-turbo"));
    }

    [Test]
    public void CreateProvider_Claude_ShouldReturnClaudeProvider()
    {
        // Act
        var provider = _factory.CreateProvider("claude");

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<ClaudeProvider>());
        Assert.That(provider.Name, Is.EqualTo("Claude"));
        Assert.That(provider.SupportedModels, Contains.Item("claude-3-5-sonnet-20241022"));
        Assert.That(provider.DefaultModel, Is.EqualTo("claude-3-5-sonnet-20241022"));
    }

    [Test]
    public void CreateProvider_Local_ShouldReturnLocalLLMProvider()
    {
        // Act
        var provider = _factory.CreateProvider("local");

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<LocalLLMProvider>());
        Assert.That(provider.Name, Is.EqualTo("LocalLLM"));
        Assert.That(provider.SupportedModels, Contains.Item("llama2"));
        Assert.That(provider.DefaultModel, Is.EqualTo("llama2"));
    }

    [Test]
    public void CreateProvider_InvalidType_ShouldThrowNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _factory.CreateProvider("invalid"));
    }

    [Test]
    public void CreateProvider_NullOrEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateProvider(""));
        Assert.Throws<ArgumentException>(() => _factory.CreateProvider(null!));
    }

    [Test]
    public void GetSupportedProviderTypes_ShouldReturnAllProviders()
    {
        // Act
        var providers = _factory.GetSupportedProviderTypes();

        // Assert
        Assert.That(providers, Is.Not.Null);
        Assert.That(providers.Count, Is.GreaterThan(0));
        Assert.That(providers, Contains.Item("openai"));
        Assert.That(providers, Contains.Item("claude"));
        Assert.That(providers, Contains.Item("local"));
        Assert.That(providers, Contains.Item("ollama"));
    }

    [Test]
    public void IsProviderSupported_ValidTypes_ShouldReturnTrue()
    {
        // Act & Assert
        Assert.That(_factory.IsProviderSupported("openai"), Is.True);
        Assert.That(_factory.IsProviderSupported("claude"), Is.True);
        Assert.That(_factory.IsProviderSupported("local"), Is.True);
        Assert.That(_factory.IsProviderSupported("ollama"), Is.True);
    }

    [Test]
    public void IsProviderSupported_InvalidTypes_ShouldReturnFalse()
    {
        // Act & Assert
        Assert.That(_factory.IsProviderSupported("invalid"), Is.False);
        Assert.That(_factory.IsProviderSupported(""), Is.False);
        Assert.That(_factory.IsProviderSupported(null!), Is.False);
    }

    [Test]
    public void CreateDefaultProvider_ShouldReturnConfiguredDefault()
    {
        // Act
        var provider = _factory.CreateDefaultProvider();

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<OpenAIProvider>());
    }

    [Test]
    public void CreateProviderForModel_GPTModel_ShouldReturnOpenAIProvider()
    {
        // Act
        var provider = _factory.CreateProviderForModel("gpt-4");

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<OpenAIProvider>());
    }

    [Test]
    public void CreateProviderForModel_ClaudeModel_ShouldReturnClaudeProvider()
    {
        // Act
        var provider = _factory.CreateProviderForModel("claude-3-sonnet-20240229");

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<ClaudeProvider>());
    }

    [Test]
    public void CreateProviderForModel_LlamaModel_ShouldReturnLocalProvider()
    {
        // Act
        var provider = _factory.CreateProviderForModel("llama2");

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<LocalLLMProvider>());
    }

    [Test]
    public async Task GetAllSupportedModelsAsync_ShouldReturnAllProviderModels()
    {
        // Act
        var allModels = await _factory.GetAllSupportedModelsAsync();

        // Assert
        Assert.That(allModels, Is.Not.Null);
        Assert.That(allModels.Count, Is.GreaterThan(0));
        
        Assert.That(allModels.ContainsKey("openai"), Is.True);
        Assert.That(allModels.ContainsKey("claude"), Is.True);
        Assert.That(allModels.ContainsKey("local"), Is.True);
        
        Assert.That(allModels["openai"], Contains.Item("gpt-4"));
        Assert.That(allModels["claude"], Contains.Item("claude-3-5-sonnet-20241022"));
        Assert.That(allModels["local"], Contains.Item("llama2"));
    }

    [Test]
    public async Task CountTokensAsync_AllProviders_ShouldReturnReasonableEstimate()
    {
        var testText = "This is a test prompt for token counting.";
        var expectedRange = (5, 15); // 예상 토큰 범위

        // Test OpenAI
        var openaiProvider = _factory.CreateProvider("openai");
        var openaiTokens = await openaiProvider.CountTokensAsync(testText);
        Assert.That(openaiTokens, Is.InRange(expectedRange.Item1, expectedRange.Item2));

        // Test Claude
        var claudeProvider = _factory.CreateProvider("claude");
        var claudeTokens = await claudeProvider.CountTokensAsync(testText);
        Assert.That(claudeTokens, Is.InRange(expectedRange.Item1, expectedRange.Item2));

        // Test Local
        var localProvider = _factory.CreateProvider("local");
        var localTokens = await localProvider.CountTokensAsync(testText);
        Assert.That(localTokens, Is.InRange(expectedRange.Item1, expectedRange.Item2));
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory?.Dispose();
    }
}

/// <summary>
/// 테스트용 HttpClientFactory
/// </summary>
internal class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}