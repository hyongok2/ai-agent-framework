namespace AIAgentFramework.LLM.Services.Explanation;

public class ExplanationResult
{
    public required string Explanation { get; init; }
    public required List<string> KeyPoints { get; init; }
    public List<string>? Examples { get; init; }
    public string? ErrorMessage { get; init; }
}
