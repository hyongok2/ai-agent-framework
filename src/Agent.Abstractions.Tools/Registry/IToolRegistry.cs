using System;
using System.Threading;
using System.Threading.Tasks;
using Agent.Abstractions.Core.Common.Identifiers;
using Agent.Abstractions.Tools.Core;
using Agent.Abstractions.Tools.Metadata;

namespace Agent.Abstractions.Tools.Registry;

/// <summary>
/// 도구 레지스트리 인터페이스
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// 도구 등록
    /// </summary>
    /// <param name="tool">도구 인스턴스</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task RegisterAsync(ITool tool, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 도구 등록 해제
    /// </summary>
    /// <param name="toolId">도구 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>제거되었으면 true</returns>
    Task<bool> UnregisterAsync(ToolId toolId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 도구 조회
    /// </summary>
    /// <param name="toolId">도구 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>도구 인스턴스, 없으면 null</returns>
    Task<ITool?> GetAsync(ToolId toolId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 도구 존재 여부 확인
    /// </summary>
    /// <param name="toolId">도구 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>존재하면 true</returns>
    Task<bool> ExistsAsync(ToolId toolId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 모든 도구 목록 조회
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>도구 설명자 목록</returns>
    Task<ToolDescriptor[]> ListAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 도구 검색
    /// </summary>
    /// <param name="criteria">검색 조건</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>매칭되는 도구 설명자 목록</returns>
    Task<ToolDescriptor[]> SearchAsync(ToolSearchCriteria criteria, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 카테고리별 도구 조회
    /// </summary>
    /// <param name="category">카테고리</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>해당 카테고리의 도구 목록</returns>
    Task<ToolDescriptor[]> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}