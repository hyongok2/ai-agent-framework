using System.Threading;
using System.Threading.Tasks;
using Agent.Abstractions.Orchestration.Configuration;

namespace Agent.Abstractions.Orchestration.Core;

/// <summary>
/// 오케스트레이션 타입 선택기
/// </summary>
public interface IOrchestrationTypeSelector
{
    /// <summary>
    /// 사용자 입력을 분석하여 적절한 오케스트레이션 타입 선택
    /// </summary>
    /// <param name="userInput">사용자 입력</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>선택된 오케스트레이션 타입</returns>
    Task<OrchestrationType> SelectAsync(
        string userInput, 
        CancellationToken cancellationToken = default);
}