using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Attributes;
using AIAgentFramework.Core.LLM.Models;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.LLM.Functions;

[LLMFunction("summarizer", "summarizer", "텍스트 요약 및 핵심 내용 추출")]
public class SummarizerFunction : LLMFunctionBase
{
    public SummarizerFunction(ILLMProvider llmProvider, IPromptManager promptManager, ILogger<SummarizerFunction> logger)
        : base(llmProvider, promptManager, logger)
    {
    }

    public override string Name => "summarizer";
    public override string Role => "summarizer";
    public override string Description => "텍스트 요약 및 핵심 내용 추출";

    protected override List<string> GetRequiredParameters()
    {
        return new List<string> { "text" };
    }

    protected override async Task<string> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken)
    {
        var parameters = PreparePromptParameters(context);
        return await _promptManager.LoadPromptAsync("summarizer", parameters);
    }

    protected override Task<LLMResult> PostProcessAsync(string response, ILLMContext context, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>
        {
            ["summary"] = response,
            ["key_points"] = ExtractKeyPoints(response),
            ["word_count"] = response.Split(' ').Length
        };
        
        var result = (LLMResult)LLMResult.CreateSuccess(response, context.Model ?? _llmProvider.DefaultModel);
        result.Data = data;
        return Task.FromResult(result);
    }

    private List<string> ExtractKeyPoints(string summary)
    {
        return summary.Split('\n')
            .Where(line => line.Trim().StartsWith("-") || line.Trim().StartsWith("•"))
            .Select(line => line.Trim().TrimStart('-', '•').Trim())
            .ToList();
    }
}