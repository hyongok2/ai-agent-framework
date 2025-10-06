namespace AIAgentFramework.LLM.Services.Refinement;

public class RefinementResult
{
    public required string RefinedContent { get; init; }
    public required List<string> Changes { get; init; }
    public required double ImprovementScore { get; init; }
    public string? ErrorMessage { get; init; }
}
