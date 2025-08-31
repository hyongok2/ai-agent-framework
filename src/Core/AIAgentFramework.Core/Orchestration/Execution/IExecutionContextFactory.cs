using System;
using AIAgentFramework.Core.Orchestration.Abstractions;

namespace AIAgentFramework.Core.Orchestration.Execution;

/// <summary>
/// 실행 컨텍스트 팩토리 인터페이스
/// </summary>
public interface IExecutionContextFactory
{
    /// <summary>
    /// 새로운 실행 컨텍스트를 생성합니다
    /// </summary>
    /// <param name="sessionId">세션 ID</param>
    /// <param name="userRequest">사용자 요청</param>
    /// <returns>실행 컨텍스트</returns>
    IExecutionContext CreateContext(string sessionId, string userRequest);

    /// <summary>
    /// 기존 오케스트레이션 컨텍스트로부터 실행 컨텍스트를 생성합니다
    /// </summary>
    /// <param name="orchestrationContext">오케스트레이션 컨텍스트</param>
    /// <returns>실행 컨텍스트</returns>
    IExecutionContext CreateContextFromOrchestration(IOrchestrationContext orchestrationContext);
}