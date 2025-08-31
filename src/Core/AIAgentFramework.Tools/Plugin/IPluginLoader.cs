namespace AIAgentFramework.Tools.Plugin;

/// <summary>
/// 플러그인 로더 인터페이스
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// 플러그인 로드
    /// </summary>
    /// <param name="pluginPath">플러그인 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>로드된 플러그인 도구들</returns>
    Task<IEnumerable<IPluginTool>> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 플러그인 매니페스트 로드
    /// </summary>
    /// <param name="manifestPath">매니페스트 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>플러그인 매니페스트</returns>
    Task<PluginManifest> LoadManifestAsync(string manifestPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 플러그인 언로드
    /// </summary>
    /// <param name="pluginId">플러그인 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>언로드 결과</returns>
    Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 로드된 플러그인 목록 조회
    /// </summary>
    /// <returns>플러그인 목록</returns>
    IEnumerable<PluginManifest> GetLoadedPlugins();
    
    /// <summary>
    /// 플러그인 검증
    /// </summary>
    /// <param name="manifest">플러그인 매니페스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 결과</returns>
    Task<bool> ValidatePluginAsync(PluginManifest manifest, CancellationToken cancellationToken = default);
}