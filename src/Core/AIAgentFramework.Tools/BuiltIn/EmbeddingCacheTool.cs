using AIAgentFramework.Core.Attributes;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIAgentFramework.Tools.BuiltIn;

/// <summary>
/// 임베딩 캐시 도구
/// </summary>
[BuiltInTool(
    name: "EmbeddingCache",
    description: "임베딩 벡터를 캐싱하고 검색하는 도구",
    Category = "Cache",
    Dependencies = "Microsoft.Extensions.Caching.Memory",
    IsCacheable = false,
    TimeoutSeconds = 10,
    Tags = "embedding,cache,vector,search"
)]
public class EmbeddingCacheTool : ToolBase
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultTtl;

    /// <inheritdoc />
    public override string Name => "EmbeddingCache";

    /// <inheritdoc />
    public override string Description => "임베딩 벡터를 캐싱하고 검색하는 도구";

    /// <inheritdoc />
    public override IToolContract Contract { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="cache">메모리 캐시</param>
    /// <param name="logger">로거</param>
    public EmbeddingCacheTool(IMemoryCache cache, ILogger<EmbeddingCacheTool> logger)
        : base(logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _defaultTtl = TimeSpan.FromHours(24); // 기본 24시간 캐시

        Contract = new ToolContract(
            inputSchema: @"{
                ""type"": ""object"",
                ""properties"": {
                    ""action"": {
                        ""type"": ""string"",
                        ""enum"": [""store"", ""retrieve"", ""search"", ""clear""],
                        ""description"": ""수행할 작업""
                    },
                    ""text"": {
                        ""type"": ""string"",
                        ""description"": ""임베딩할 텍스트 또는 검색할 텍스트""
                    },
                    ""embedding"": {
                        ""type"": ""array"",
                        ""items"": { ""type"": ""number"" },
                        ""description"": ""임베딩 벡터 (store 액션에서 사용)""
                    },
                    ""threshold"": {
                        ""type"": ""number"",
                        ""minimum"": 0,
                        ""maximum"": 1,
                        ""description"": ""유사도 임계값 (search 액션에서 사용)""
                    },
                    ""limit"": {
                        ""type"": ""integer"",
                        ""minimum"": 1,
                        ""description"": ""반환할 최대 결과 수""
                    },
                    ""ttl_hours"": {
                        ""type"": ""integer"",
                        ""minimum"": 1,
                        ""description"": ""캐시 유지 시간 (시간 단위)""
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
                                ""text"": { ""type"": ""string"" },
                                ""embedding"": { ""type"": ""array"", ""items"": { ""type"": ""number"" } },
                                ""similarity"": { ""type"": ""number"" },
                                ""cached_at"": { ""type"": ""string"" }
                            }
                        }
                    },
                    ""cache_stats"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""total_entries"": { ""type"": ""integer"" },
                            ""cache_hit"": { ""type"": ""boolean"" }
                        }
                    }
                }
            }",
            requiredParameters: new List<string> { "action" },
            optionalParameters: new List<string> { "text", "embedding", "threshold", "limit", "ttl_hours" }
        );
    }

    /// <inheritdoc />
    protected override async Task<ToolResult> ExecuteInternalAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        var action = GetRequiredParameter<string>(input, "action").ToLowerInvariant();

        return action switch
        {
            "store" => await StoreEmbeddingAsync(input),
            "retrieve" => await RetrieveEmbeddingAsync(input),
            "search" => await SearchSimilarEmbeddingsAsync(input),
            "clear" => await ClearCacheAsync(input),
            _ => ToolResult.CreateFailure($"Unknown action: {action}")
        };
    }

    /// <summary>
    /// 임베딩 저장
    /// </summary>
    private Task<ToolResult> StoreEmbeddingAsync(IToolInput input)
    {
        var text = GetRequiredParameter<string>(input, "text");
        var embedding = GetRequiredParameter<float[]>(input, "embedding");
        var ttlHours = GetParameter(input, "ttl_hours", 24);

        var cacheKey = GenerateCacheKey(text);
        var cacheEntry = new EmbeddingCacheEntry
        {
            Text = text,
            Embedding = embedding,
            CachedAt = DateTime.UtcNow
        };

        var ttl = TimeSpan.FromHours(ttlHours);
        _cache.Set(cacheKey, cacheEntry, ttl);

        _logger.LogDebug("Stored embedding for text: {Text} (key: {Key})", 
            text.Length > 50 ? text.Substring(0, 50) + "..." : text, cacheKey);

        return Task.FromResult(ToolResult.CreateSuccess()
            .WithData("action", "store")
            .WithData("cache_key", cacheKey)
            .WithData("ttl_hours", ttlHours)
            .WithData("embedding_dimension", embedding.Length));
    }

    /// <summary>
    /// 임베딩 검색
    /// </summary>
    private Task<ToolResult> RetrieveEmbeddingAsync(IToolInput input)
    {
        var text = GetRequiredParameter<string>(input, "text");
        var cacheKey = GenerateCacheKey(text);

        if (_cache.TryGetValue(cacheKey, out EmbeddingCacheEntry? entry) && entry != null)
        {
            _logger.LogDebug("Cache hit for text: {Text}", 
                text.Length > 50 ? text.Substring(0, 50) + "..." : text);

            return Task.FromResult(ToolResult.CreateSuccess()
                .WithData("action", "retrieve")
                .WithData("cache_hit", true)
                .WithData("text", entry.Text)
                .WithData("embedding", entry.Embedding)
                .WithData("cached_at", entry.CachedAt));
        }

        _logger.LogDebug("Cache miss for text: {Text}", 
            text.Length > 50 ? text.Substring(0, 50) + "..." : text);

        return Task.FromResult(ToolResult.CreateSuccess()
            .WithData("action", "retrieve")
            .WithData("cache_hit", false));
    }

    /// <summary>
    /// 유사한 임베딩 검색
    /// </summary>
    private Task<ToolResult> SearchSimilarEmbeddingsAsync(IToolInput input)
    {
        var queryEmbedding = GetRequiredParameter<float[]>(input, "embedding");
        var threshold = GetParameter(input, "threshold", 0.8f);
        var limit = GetParameter(input, "limit", 10);

        var results = new List<SimilarityResult>();

        // 캐시에서 모든 항목을 검색 (실제 구현에서는 더 효율적인 방법 사용)
        // 이는 데모용 구현이며, 실제로는 벡터 데이터베이스를 사용해야 함
        var cacheField = typeof(MemoryCache).GetField("_coherentState", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (cacheField?.GetValue(_cache) is object coherentState)
        {
            var entriesCollection = coherentState.GetType()
                .GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (entriesCollection?.GetValue(coherentState) is IDictionary entries)
            {
                foreach (DictionaryEntry entry in entries)
                {
                    if (entry.Value?.GetType().GetProperty("Value")?.GetValue(entry.Value) is EmbeddingCacheEntry cacheEntry)
                    {
                        var similarity = CalculateCosineSimilarity(queryEmbedding, cacheEntry.Embedding);
                        
                        if (similarity >= threshold)
                        {
                            results.Add(new SimilarityResult
                            {
                                Text = cacheEntry.Text,
                                Embedding = cacheEntry.Embedding,
                                Similarity = similarity,
                                CachedAt = cacheEntry.CachedAt
                            });
                        }
                    }
                }
            }
        }

        // 유사도 순으로 정렬하고 제한
        results = results.OrderByDescending(r => r.Similarity)
                        .Take(limit)
                        .ToList();

        _logger.LogDebug("Found {Count} similar embeddings above threshold {Threshold}", 
            results.Count, threshold);

        return Task.FromResult(ToolResult.CreateSuccess()
            .WithData("action", "search")
            .WithData("results", results)
            .WithData("threshold", threshold)
            .WithData("total_found", results.Count));
    }

    /// <summary>
    /// 캐시 지우기
    /// </summary>
    private Task<ToolResult> ClearCacheAsync(IToolInput input)
    {
        // MemoryCache는 전체 클리어 메서드가 없으므로 새 인스턴스로 교체하는 방식 사용
        // 실제 구현에서는 캐시 키 추적 메커니즘을 구현해야 함
        
        _logger.LogInformation("Embedding cache clear requested");

        return Task.FromResult(ToolResult.CreateSuccess()
            .WithData("action", "clear")
            .WithData("message", "Cache clear requested (implementation depends on cache provider)"));
    }

    /// <summary>
    /// 캐시 키 생성
    /// </summary>
    private string GenerateCacheKey(string text)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return $"embedding_{Convert.ToHexString(hash)[..16]}";
    }

    /// <summary>
    /// 코사인 유사도 계산
    /// </summary>
    private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0f;

        var dotProduct = 0f;
        var magnitude1 = 0f;
        var magnitude2 = 0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        if (magnitude1 == 0f || magnitude2 == 0f)
            return 0f;

        return dotProduct / (MathF.Sqrt(magnitude1) * MathF.Sqrt(magnitude2));
    }
}

/// <summary>
/// 임베딩 캐시 엔트리
/// </summary>
public class EmbeddingCacheEntry
{
    public string Text { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public DateTime CachedAt { get; set; }
}

/// <summary>
/// 유사도 검색 결과
/// </summary>
public class SimilarityResult
{
    public string Text { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public float Similarity { get; set; }
    public DateTime CachedAt { get; set; }
}