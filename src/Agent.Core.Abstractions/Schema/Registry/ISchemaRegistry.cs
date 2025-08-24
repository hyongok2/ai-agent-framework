using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Agent.Core.Abstractions.Schema.Registry;

/// <summary>
/// 스키마 레지스트리 인터페이스
/// </summary>
public interface ISchemaRegistry
{
    /// <summary>
    /// 스키마 등록
    /// </summary>
    /// <param name="schemaId">스키마 ID</param>
    /// <param name="schema">JSON Schema</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task RegisterAsync(string schemaId, JsonNode schema, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 스키마 조회
    /// </summary>
    /// <param name="schemaId">스키마 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스키마가 존재하면 JsonNode, 없으면 null</returns>
    Task<JsonNode?> GetAsync(string schemaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 스키마 존재 여부 확인
    /// </summary>
    /// <param name="schemaId">스키마 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>존재하면 true</returns>
    Task<bool> ExistsAsync(string schemaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 스키마 제거
    /// </summary>
    /// <param name="schemaId">스키마 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>제거되었으면 true</returns>
    Task<bool> RemoveAsync(string schemaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 모든 스키마 ID 조회
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스키마 ID 목록</returns>
    Task<string[]> GetAllIdsAsync(CancellationToken cancellationToken = default);
}

