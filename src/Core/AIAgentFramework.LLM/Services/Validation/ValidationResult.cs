namespace AIAgentFramework.LLM.Services.Validation;

public class ValidationResult
{
    public required bool IsValid { get; init; }
    public List<ValidationError>? Errors { get; init; }
    public List<string>? Warnings { get; init; }
    public string? ErrorMessage { get; init; }
}

public class ValidationError
{
    public required string Field { get; init; }
    public required string Message { get; init; }
    public string? Severity { get; init; }
}
