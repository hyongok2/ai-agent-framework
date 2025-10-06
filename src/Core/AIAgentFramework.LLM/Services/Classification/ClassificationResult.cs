namespace AIAgentFramework.LLM.Services.Classification;

public class ClassificationResult
{
    public required string PrimaryCategory { get; init; }
    public required double Confidence { get; init; }
    public List<CategoryScore>? AlternativeCategories { get; init; }
    public string? Reasoning { get; init; }
    public string? ErrorMessage { get; init; }
}

public class CategoryScore
{
    public required string Category { get; init; }
    public required double Score { get; init; }
}
