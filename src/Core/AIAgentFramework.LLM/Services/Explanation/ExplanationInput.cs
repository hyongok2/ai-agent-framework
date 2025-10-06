namespace AIAgentFramework.LLM.Services.Explanation;

public class ExplanationInput
{
    public required string Topic { get; init; }
    public string? AudienceLevel { get; init; }
    public string? Focus { get; init; }
}
