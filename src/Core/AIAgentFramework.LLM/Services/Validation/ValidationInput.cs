namespace AIAgentFramework.LLM.Services.Validation;

public class ValidationInput
{
    public required string Content { get; init; }
    public required string Schema { get; init; }
    public string? Rules { get; init; }
}
