namespace Agent.Core.Abstractions.Common.Exceptions;
/// <summary>
/// 스키마 검증 실패 예외
/// </summary>
public class SchemaValidationException : AgentException
{
    public string SchemaId { get; }
    public string[] Errors { get; }
    
    public SchemaValidationException(string schemaId, string[] errors)
        : base($"Schema validation failed for {schemaId}: {string.Join(", ", errors)}", "SCHEMA_VALIDATION_FAILED")
    {
        SchemaId = schemaId;
        Errors = errors;
    }
}