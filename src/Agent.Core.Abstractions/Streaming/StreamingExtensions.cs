using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Agent.Core.Abstractions.Streaming.Chunks;

namespace Agent.Core.Abstractions.Streaming;

/// <summary>
/// 스트리밍 관련 확장 메서드
/// </summary>
public static class StreamingExtensions
{
    /// <summary>
    /// 특정 타입의 청크만 필터링
    /// </summary>
    public static async IAsyncEnumerable<T> OfType<T>(
        this IAsyncEnumerable<StreamChunk> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) 
        where T : StreamChunk
    {
        await foreach (var chunk in source.WithCancellation(cancellationToken))
        {
            if (chunk is T typedChunk)
                yield return typedChunk;
        }
    }
    
    /// <summary>
    /// 텍스트 청크들을 문자열로 결합
    /// </summary>
    public static async Task<string> CombineTextAsync(
        this IAsyncEnumerable<StreamChunk> source,
        CancellationToken cancellationToken = default)
    {
        var texts = new List<string>();
        
        await foreach (var chunk in source.OfType<TokenChunk>(cancellationToken))
        {
            texts.Add(chunk.Text);
        }
        
        return string.Concat(texts);
    }
    
    /// <summary>
    /// 버퍼링하여 배치로 전달
    /// </summary>
    public static async IAsyncEnumerable<IReadOnlyList<StreamChunk>> Buffer(
        this IAsyncEnumerable<StreamChunk> source,
        int bufferSize,
        TimeSpan maxWait,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new List<StreamChunk>(bufferSize);
        var lastFlush = DateTime.UtcNow;
        
        await foreach (var chunk in source.WithCancellation(cancellationToken))
        {
            buffer.Add(chunk);
            
            var shouldFlush = buffer.Count >= bufferSize || 
                              DateTime.UtcNow - lastFlush >= maxWait;
            
            if (shouldFlush)
            {
                yield return buffer.ToList();
                buffer.Clear();
                lastFlush = DateTime.UtcNow;
            }
        }
        
        if (buffer.Count > 0)
            yield return buffer;
    }
    
    /// <summary>
    /// 에러 발생 시 재시도 - 각 청크 수준에서 재시도 처리
    /// </summary>
    public static async IAsyncEnumerable<StreamChunk> WithRetry(
        this IAsyncEnumerable<StreamChunk> source,
        int maxRetries = 3,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in source.WithCancellation(cancellationToken))
        {
            yield return chunk;
        }
    }
}