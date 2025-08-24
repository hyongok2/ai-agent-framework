using Agent.Abstractions.Core.Common.Identifiers;

namespace Agent.Abstractions.Core.Memory;

/// <summary>
/// 메모리 관리자 인터페이스
/// </summary>
public interface IMemoryManager : IDisposable, IAsyncDisposable
{
    #region 대화 기록 관리
    
    /// <summary>
    /// 대화 기록 저장
    /// </summary>
    /// <param name="entry">대화 항목</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>저장된 항목 ID</returns>
    Task<string> StoreConversationAsync(ConversationEntry entry, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 대화 기록 조회
    /// </summary>
    /// <param name="limit">최대 항목 수</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>대화 기록</returns>
    Task<IReadOnlyList<ConversationEntry>> GetConversationHistoryAsync(int limit = 50, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 대화 기록 삭제
    /// </summary>
    /// <param name="entryId">항목 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>삭제 성공 여부</returns>
    Task<bool> DeleteConversationEntryAsync(string entryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 모든 대화 기록 삭제
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>삭제된 항목 수</returns>
    Task<int> ClearConversationHistoryAsync(CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 컨텍스트 관리
    
    /// <summary>
    /// 현재 컨텍스트 조회
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>현재 컨텍스트</returns>
    Task<MemoryContext> GetCurrentContextAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 컨텍스트 업데이트
    /// </summary>
    /// <param name="context">새 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>업데이트 성공 여부</returns>
    Task<bool> UpdateContextAsync(MemoryContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 컨텍스트 압축 (토큰 수 줄이기)
    /// </summary>
    /// <param name="targetTokenCount">목표 토큰 수</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>압축된 컨텍스트</returns>
    Task<MemoryContext> CompressContextAsync(int targetTokenCount, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 임시 데이터 저장
    
    /// <summary>
    /// 키-값 데이터 저장
    /// </summary>
    /// <typeparam name="T">데이터 타입</typeparam>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <param name="expiry">만료 시간</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>저장 성공 여부</returns>
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 키로 데이터 조회
    /// </summary>
    /// <typeparam name="T">데이터 타입</typeparam>
    /// <param name="key">키</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>데이터 (없으면 default)</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 키 존재 확인
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>존재 여부</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 키 삭제
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>삭제 성공 여부</returns>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 메모리 통계
    
    /// <summary>
    /// 메모리 사용 통계
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>메모리 통계</returns>
    Task<MemoryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 메모리 정리 (만료된 데이터 삭제)
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>정리된 항목 수</returns>
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
    
    #endregion
}