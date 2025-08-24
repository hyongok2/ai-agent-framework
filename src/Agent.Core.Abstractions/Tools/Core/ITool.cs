using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Agent.Core.Abstractions.Common.Identifiers;
using Agent.Core.Abstractions.Tools.Execution;
using Agent.Core.Abstractions.Tools.Metadata;

namespace Agent.Core.Abstractions.Tools.Core;

/// <summary>
/// 도구 실행 인터페이스
/// </summary>
public interface ITool
{
    /// <summary>
    /// 도구 정보 반환
    /// </summary>
    /// <returns>도구 설명자</returns>
    ToolDescriptor Describe();
    
    /// <summary>
    /// 도구 실행
    /// </summary>
    /// <param name="input">입력 데이터</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<ToolResult> ExecuteAsync(JsonNode input, ToolContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 입력 검증
    /// </summary>
    /// <param name="input">입력 데이터</param>
    /// <returns>유효한 경우 true</returns>
    Task<bool> ValidateInputAsync(JsonNode input);
    
    /// <summary>
    /// 도구 초기화
    /// </summary>
    /// <param name="configuration">초기화 설정</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task InitializeAsync(JsonNode? configuration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 도구 정리
    /// </summary>
    Task DisposeAsync();
}



