namespace AIAgentFramework.LLM.Services.Refinement;

public class RefinementInput
{
    public required string OriginalContent { get; init; }
    public required string Purpose { get; init; }
    public string? Feedback { get; init; }
}
