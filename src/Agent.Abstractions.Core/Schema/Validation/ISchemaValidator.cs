using System.Text.Json.Nodes;

namespace Agent.Abstractions.Core.Schema.Validation;

/// <summary>
/// JSON Schema 검증 인터페이스
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// JSON이 스키마를 준수하는지 검증
    /// </summary>
    /// <param name="json">검증할 JSON</param>
    /// <param name="schemaId">스키마 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>유효한 경우 true</returns>
    Task<bool> ValidateAsync(JsonNode json, string schemaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// JSON을 스키마에 맞게 강제 변환
    /// </summary>
    /// <param name="json">변환할 JSON</param>
    /// <param name="schemaId">스키마 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>변환된 JSON</returns>
    Task<JsonNode> CoerceAsync(JsonNode json, string schemaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 검증 오류 상세 정보 가져오기
    /// </summary>
    /// <param name="json">검증할 JSON</param>
    /// <param name="schemaId">스키마 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 결과</returns>
    Task<ValidationResult> ValidateWithDetailsAsync(JsonNode json, string schemaId, CancellationToken cancellationToken = default);
}




