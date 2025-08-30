using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.LLM.Functions;

[LLMFunction("interpreter", "interpreter", "사용자 입력 해석 및 의도 파악")]
public class InterpreterFunction : LLMFunctionBase
{
    public InterpreterFunction(ILLMProvider llmProvider, IPromptManager promptManager, ILogger<InterpreterFunction> logger)
        : base(llmProvider, promptManager, logger)
    {
    }

    public override string Name => "interpreter";
    public override string Role => "interpreter";
    public override string Description => "사용자 입력 해석 및 의도 파악";

    protected override List<string> GetRequiredParameters()
    {
        return new List<string> { "user_input" };
    }

    protected override async Task<string> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken)
    {
        var parameters = PreparePromptParameters(context);
        return await _promptManager.LoadPromptAsync("interpreter", parameters);
    }

    protected override Task<LLMResult> PostProcessAsync(string response, ILLMContext context, CancellationToken cancellationToken)
    {
        var result = (LLMResult)LLMResult.CreateSuccess(response, context.Model ?? _llmProvider.DefaultModel);
        result.Data = ParseInterpreterResponse(response);
        return Task.FromResult(result);
    }

    private Dictionary<string, object> ParseInterpreterResponse(string response)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                response, 
                @"```json\s*(.*?)\s*```", 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            var json = jsonMatch.Success ? jsonMatch.Groups[1].Value : response;
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return data ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>
            {
                ["interpretation"] = response,
                ["intent"] = "unknown",
                ["keywords"] = new List<string>()
            };
        }
    }
}