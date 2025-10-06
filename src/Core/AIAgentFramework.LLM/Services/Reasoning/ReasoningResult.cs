namespace AIAgentFramework.LLM.Services.Reasoning;

public class ReasoningResult
{
    public required string Conclusion { get; init; }
    public required List<string> ReasoningSteps { get; init; }
    public required double Confidence { get; init; }
    public List<string>? Assumptions { get; init; }
    public string? ErrorMessage { get; init; }
}
