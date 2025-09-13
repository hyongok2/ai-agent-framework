namespace AIAgentFramework.Core.Infrastructure.Configuration;

/// <summary>
/// 설정 관리자 인터페이스
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// 설정 섹션을 조회합니다.
    /// </summary>
    /// <typeparam name="T">설정 타입</typeparam>
    /// <param name="sectionName">섹션 이름</param>
    /// <returns>설정 객체</returns>
    T? GetSection<T>(string sectionName) where T : class;
    
    /// <summary>
    /// 설정 값을 조회합니다.
    /// </summary>
    /// <param name="key">설정 키</param>
    /// <returns>설정 값</returns>
    string? GetValue(string key);
    
    /// <summary>
    /// 설정 값을 조회합니다 (기본값 포함).
    /// </summary>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="key">설정 키</param>
    /// <param name="defaultValue">기본값</param>
    /// <returns>설정 값</returns>
    T GetValue<T>(string key, T defaultValue);
    
    /// <summary>
    /// 설정을 다시 로드합니다.
    /// </summary>
    /// <returns>재로드 작업</returns>
    Task ReloadConfigurationAsync();
    
    /// <summary>
    /// 설정 변경 이벤트를 구독합니다.
    /// </summary>
    /// <param name="callback">변경 콜백</param>
    /// <returns>구독 해제 토큰</returns>
    IDisposable OnConfigurationChanged(Action<string> callback);
}