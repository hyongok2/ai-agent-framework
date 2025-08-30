using System.Threading;
using System.Threading.Tasks;

namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 오케스트레이션 전략 인터페이스
/// 다양한 실행 전략을 구현할 수 있도록 하는 전략 패턴
/// </summary>
public interface IOrchestrationStrategy
{
    /// <summary>
    /// 전략 이름
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 전략 설명
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 이 전략이 주어진 컨텍스트에 적합한지 판단
    /// </summary>
    bool CanHandle(IOrchestrationContext context);
    
    /// <summary>
    /// 전략 실행
    /// </summary>
    Task<IOrchestrationResult> ExecuteAsync(
        IOrchestrationContext context, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 전략 실행 우선순위 (높을수록 우선)
    /// </summary>
    int Priority { get; }
}