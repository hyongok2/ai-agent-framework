

using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Attributes;
using AIAgentFramework.Core.LLM.Models;
using AIAgentFramework.Registry;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.LLM.Functions;

[LLMFunction("generator", "generator", "창작 및 콘텐츠 생성")]
public class GeneratorFunction : LLMFunctionBase
{
    public GeneratorFunction(ILLMProvider llmProvider, IPromptManager promptManager, ILogger<GeneratorFunction> logger, IAdvancedRegistry registry)
        : base(llmProvider, promptManager, logger, registry)
    {
    }

    public override string Name => "generator";
    public override string Role => "generator";
    public override string Description => "창작 및 콘텐츠 생성";

    protected override List<string> GetRequiredParameters()
    {
        return new List<string> { "generation_request" };
    }

    protected override async Task<string> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken)
    {
        var parameters = PreparePromptParameters(context);
        return await _promptManager.LoadPromptAsync("generator", parameters);
    }

    protected override Task<LLMResult> PostProcessAsync(string response, ILLMContext context, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>
        {
            ["generated_content"] = response,
            ["content_type"] = context.Parameters.GetValueOrDefault("content_type", "text"),
            ["length"] = response.Length
        };
        
        var result = (LLMResult)LLMResult.CreateSuccess(response, context.Model ?? _llmProvider.DefaultModel);
        result.Data = data;
        return Task.FromResult(result);
    }
}