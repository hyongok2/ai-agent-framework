namespace AIAgentFramework.LLM.Services.Classification;

public class ClassificationInput
{
    public required string Content { get; init; }
    public required List<string> Categories { get; init; }
    public string? Context { get; init; }
}
