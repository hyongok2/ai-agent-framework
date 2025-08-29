using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Tools.Plugin;

/// <summary>
/// 플러그인 관리자 인터페이스
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// 플러그인 디렉토리에서 모든 플러그인 로드
    /// </summary>
    /// <param name="pluginDirectory">플러그인 디렉토리</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>로드된 플러그인 수</returns>
    Task<int> LoadPluginsFromDirectoryAsync(string pluginDirectory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 특정 플러그인 로드
    /// </summary>
    /// <param name="pluginPath">플러그인 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>로드된 도구 목록</returns>
    Task<IEnumerable<IPluginTool>> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 플러그인 언로드
    /// </summary>
    /// <param name="pluginId">플러그인 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>언로드 결과</returns>
    Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 플러그인 도구 조회
    /// </summary>
    /// <param name="toolName">도구 이름</param>
    /// <returns>플러그인 도구</returns>
    IPluginTool? GetPluginTool(string toolName);
    
    /// <summary>
    /// 모든 플러그인 도구 조회
    /// </summary>
    /// <returns>플러그인 도구 목록</returns>
    IEnumerable<IPluginTool> GetAllPluginTools();
    
    /// <summary>
    /// 로드된 플러그인 목록 조회
    /// </summary>
    /// <returns>플러그인 매니페스트 목록</returns>
    IEnumerable<PluginManifest> GetLoadedPlugins();
    
    /// <summary>
    /// 플러그인 설정 업데이트
    /// </summary>
    /// <param name="pluginId">플러그인 ID</param>
    /// <param name="configuration">새 설정</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>업데이트 결과</returns>
    Task<bool> UpdatePluginConfigurationAsync(string pluginId, Dictionary<string, object> configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 플러그인 상태 조회
    /// </summary>
    /// <param name="pluginId">플러그인 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>플러그인 상태</returns>
    Task<Dictionary<string, object>?> GetPluginStatusAsync(string pluginId, CancellationToken cancellationToken = default);
}