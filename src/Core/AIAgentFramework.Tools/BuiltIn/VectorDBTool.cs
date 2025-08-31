


using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Tools.Attributes;
using AIAgentFramework.Core.Tools.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.Tools.BuiltIn;

/// <summary>
/// 벡터 데이터베이스 도구
/// </summary>
[BuiltInTool(
    name: "VectorDB",
    description: "벡터 데이터베이스에서 유사도 검색을 수행하는 도구",
    Category = "Database",
    Dependencies = "VectorDatabase",
    TimeoutSeconds = 30,
    IsCacheable = true,
    CacheTTLMinutes = 15,
    Tags = "vector,database,search,rag,similarity"
)]
public class VectorDBTool : ToolBase
{
    private readonly IVectorDatabase _vectorDb;

    /// <inheritdoc />
    public override string Name => "VectorDB";

    /// <inheritdoc />
    public override string Description => "벡터 데이터베이스에서 유사도 검색을 수행하는 도구";

    /// <inheritdoc />
    public override IToolContract Contract { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="vectorDb">벡터 데이터베이스</param>
    /// <param name="logger">로거</param>
    public VectorDBTool(IVectorDatabase vectorDb, ILogger<VectorDBTool> logger)
        : base(logger)
    {
        _vectorDb = vectorDb ?? throw new ArgumentNullException(nameof(vectorDb));

        Contract = new ToolContract(
            inputSchema: @"{
                ""type"": ""object"",
                ""properties"": {
                    ""action"": {
                        ""type"": ""string"",
                        ""enum"": [""search"", ""insert"", ""update"", ""delete"", ""get_collections""],
                        ""description"": ""수행할 작업""
                    },
                    ""collection"": {
                        ""type"": ""string"",
                        ""description"": ""컬렉션 이름""
                    },
                    ""query_vector"": {
                        ""type"": ""array"",
                        ""items"": { ""type"": ""number"" },
                        ""description"": ""검색할 벡터""
                    },
                    ""query_text"": {
                        ""type"": ""string"",
                        ""description"": ""검색할 텍스트 (벡터로 변환됨)""
                    },
                    ""limit"": {
                        ""type"": ""integer"",
                        ""minimum"": 1,
                        ""maximum"": 100,
                        ""description"": ""반환할 최대 결과 수""
                    },
                    ""threshold"": {
                        ""type"": ""number"",
                        ""minimum"": 0,
                        ""maximum"": 1,
                        ""description"": ""유사도 임계값""
                    },
                    ""filters"": {
                        ""type"": ""object"",
                        ""description"": ""메타데이터 필터""
                    },
                    ""document"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""string"" },
                            ""text"": { ""type"": ""string"" },
                            ""vector"": { ""type"": ""array"", ""items"": { ""type"": ""number"" } },
                            ""metadata"": { ""type"": ""object"" }
                        },
                        ""description"": ""삽입/업데이트할 문서""
                    }
                },
                ""required"": [""action""]
            }",
            outputSchema: @"{
                ""type"": ""object"",
                ""properties"": {
                    ""success"": { ""type"": ""boolean"" },
                    ""action"": { ""type"": ""string"" },
                    ""results"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""id"": { ""type"": ""string"" },
                                ""text"": { ""type"": ""string"" },
                                ""score"": { ""type"": ""number"" },
                                ""metadata"": { ""type"": ""object"" }
                            }
                        }
                    },
                    ""total_results"": { ""type"": ""integer"" },
                    ""query_time_ms"": { ""type"": ""number"" }
                }
            }",
            requiredParameters: new List<string> { "action" },
            optionalParameters: new List<string> { "collection", "query_vector", "query_text", "limit", "threshold", "filters", "document" }
        );
    }

    /// <inheritdoc />
    protected override async Task<ToolResult> ExecuteInternalAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        var action = GetRequiredParameter<string>(input, "action").ToLowerInvariant();

        return action switch
        {
            "search" => await SearchVectorsAsync(input, cancellationToken),
            "insert" => await InsertDocumentAsync(input, cancellationToken),
            "update" => await UpdateDocumentAsync(input, cancellationToken),
            "delete" => await DeleteDocumentAsync(input, cancellationToken),
            "get_collections" => await GetCollectionsAsync(input, cancellationToken),
            _ => ToolResult.CreateFailure($"Unknown action: {action}")
        };
    }

    /// <summary>
    /// 벡터 검색
    /// </summary>
    private async Task<ToolResult> SearchVectorsAsync(IToolInput input, CancellationToken cancellationToken)
    {
        var collection = GetParameter(input, "collection", "default");
        var limit = GetParameter(input, "limit", 10);
        var threshold = GetParameter(input, "threshold", 0.0f);
        var filters = GetParameter<Dictionary<string, object>>(input, "filters");

        // 쿼리 벡터 또는 텍스트 중 하나는 필요
        var queryVector = GetParameter<float[]>(input, "query_vector");
        var queryText = GetParameter<string>(input, "query_text");

        if (queryVector == null && string.IsNullOrEmpty(queryText))
        {
            return ToolResult.CreateFailure("Either query_vector or query_text must be provided");
        }

        var startTime = DateTime.UtcNow;

        try
        {
            VectorSearchResult searchResult;

            if (queryVector != null)
            {
                searchResult = await _vectorDb.SearchByVectorAsync(collection, queryVector, limit, threshold, filters, cancellationToken);
            }
            else
            {
                searchResult = await _vectorDb.SearchByTextAsync(collection, queryText!, limit, threshold, filters, cancellationToken);
            }

            var queryTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogDebug("Vector search completed: {ResultCount} results in {QueryTime}ms", 
                searchResult.Results.Count, queryTime);

            return ToolResult.CreateSuccess()
                .WithData("action", "search")
                .WithData("results", searchResult.Results)
                .WithData("total_results", searchResult.TotalResults)
                .WithData("query_time_ms", queryTime)
                .WithData("collection", collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed");
            return ToolResult.CreateFailure($"Search failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 문서 삽입
    /// </summary>
    private async Task<ToolResult> InsertDocumentAsync(IToolInput input, CancellationToken cancellationToken)
    {
        var collection = GetParameter(input, "collection", "default");
        var document = GetRequiredParameter<Dictionary<string, object>>(input, "document");

        try
        {
            var vectorDocument = ParseVectorDocument(document);
            var documentId = await _vectorDb.InsertDocumentAsync(collection, vectorDocument, cancellationToken);

            _logger.LogDebug("Document inserted: {DocumentId} in collection {Collection}", documentId, collection);

            return ToolResult.CreateSuccess()
                .WithData("action", "insert")
                .WithData("document_id", documentId)
                .WithData("collection", collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document insertion failed");
            return ToolResult.CreateFailure($"Insert failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 문서 업데이트
    /// </summary>
    private async Task<ToolResult> UpdateDocumentAsync(IToolInput input, CancellationToken cancellationToken)
    {
        var collection = GetParameter(input, "collection", "default");
        var document = GetRequiredParameter<Dictionary<string, object>>(input, "document");

        try
        {
            var vectorDocument = ParseVectorDocument(document);
            var success = await _vectorDb.UpdateDocumentAsync(collection, vectorDocument, cancellationToken);

            _logger.LogDebug("Document update result: {Success} for document {DocumentId}", success, vectorDocument.Id);

            return ToolResult.CreateSuccess()
                .WithData("action", "update")
                .WithData("success", success)
                .WithData("document_id", vectorDocument.Id)
                .WithData("collection", collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document update failed");
            return ToolResult.CreateFailure($"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 문서 삭제
    /// </summary>
    private async Task<ToolResult> DeleteDocumentAsync(IToolInput input, CancellationToken cancellationToken)
    {
        var collection = GetParameter(input, "collection", "default");
        var documentId = GetRequiredParameter<string>(input, "document_id");

        try
        {
            var success = await _vectorDb.DeleteDocumentAsync(collection, documentId, cancellationToken);

            _logger.LogDebug("Document deletion result: {Success} for document {DocumentId}", success, documentId);

            return ToolResult.CreateSuccess()
                .WithData("action", "delete")
                .WithData("success", success)
                .WithData("document_id", documentId)
                .WithData("collection", collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document deletion failed");
            return ToolResult.CreateFailure($"Delete failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 컬렉션 목록 조회
    /// </summary>
    private async Task<ToolResult> GetCollectionsAsync(IToolInput input, CancellationToken cancellationToken)
    {
        try
        {
            var collections = await _vectorDb.GetCollectionsAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} collections", collections.Count);

            return ToolResult.CreateSuccess()
                .WithData("action", "get_collections")
                .WithData("collections", collections)
                .WithData("total_collections", collections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections");
            return ToolResult.CreateFailure($"Get collections failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 딕셔너리를 VectorDocument로 변환
    /// </summary>
    private VectorDocument ParseVectorDocument(Dictionary<string, object> document)
    {
        var id = document.TryGetValue("id", out var idValue) ? idValue?.ToString() : Guid.NewGuid().ToString();
        var text = document.TryGetValue("text", out var textValue) ? textValue?.ToString() : string.Empty;
        
        var vector = Array.Empty<float>();
        if (document.TryGetValue("vector", out var vectorValue) && vectorValue is JsonElement vectorElement)
        {
            vector = vectorElement.EnumerateArray().Select(e => e.GetSingle()).ToArray();
        }

        var metadata = new Dictionary<string, object>();
        if (document.TryGetValue("metadata", out var metadataValue) && metadataValue is Dictionary<string, object> metaDict)
        {
            metadata = metaDict;
        }

        return new VectorDocument
        {
            Id = id!,
            Text = text!,
            Vector = vector,
            Metadata = metadata
        };
    }
}

/// <summary>
/// 벡터 데이터베이스 인터페이스 (실제 구현에서는 별도 프로젝트에 위치)
/// </summary>
public interface IVectorDatabase
{
    Task<VectorSearchResult> SearchByVectorAsync(string collection, float[] queryVector, int limit, float threshold, Dictionary<string, object>? filters, CancellationToken cancellationToken);
    Task<VectorSearchResult> SearchByTextAsync(string collection, string queryText, int limit, float threshold, Dictionary<string, object>? filters, CancellationToken cancellationToken);
    Task<string> InsertDocumentAsync(string collection, VectorDocument document, CancellationToken cancellationToken);
    Task<bool> UpdateDocumentAsync(string collection, VectorDocument document, CancellationToken cancellationToken);
    Task<bool> DeleteDocumentAsync(string collection, string documentId, CancellationToken cancellationToken);
    Task<List<string>> GetCollectionsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// 벡터 문서
/// </summary>
public class VectorDocument
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public float[] Vector { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 벡터 검색 결과
/// </summary>
public class VectorSearchResult
{
    public List<VectorSearchItem> Results { get; set; } = new();
    public int TotalResults { get; set; }
}

/// <summary>
/// 벡터 검색 항목
/// </summary>
public class VectorSearchItem
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public float Score { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}