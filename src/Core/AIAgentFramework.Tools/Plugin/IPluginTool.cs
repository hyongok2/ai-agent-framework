using AIAgentFramework.Core.Tools.Abstractions;

namespace AIAgentFramework.Tools.Plugin;

/// <summary>
/// 플러그인 도구 인터페이스
/// </summary>
public interface IPluginTool : ITool
{
    /// <summary>
    /// 플러그인 버전
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// 플러그인 작성자
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// 플러그인 의존성 정보
    /// </summary>
    IEnumerable<string> Dependencies { get; }
    
    /// <summary>
    /// 플러그인 초기화
    /// </summary>
    /// <param name="configuration">플러그인 설정</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>초기화 결과</returns>
    Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 플러그인 정리
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>정리 작업</returns>
    Task DisposeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 플러그인 상태 확인
    /// </summary>
    /// <returns>상태 정보</returns>
    Task<Dictionary<string, object>> GetStatusAsync();
}