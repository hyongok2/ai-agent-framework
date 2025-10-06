namespace AIAgentFramework.LLM.Services.Reasoning;

public class ReasoningInput
{
    public required string Problem { get; init; }
    public string? Facts { get; init; }
    public string? Rules { get; init; }
}
