using AIAgentFramework.Configuration.Models;

namespace AIAgentFramework.Configuration;

/// <summary>
/// AI 에이전트 설정 관리자 인터페이스
/// </summary>
public interface IAIAgentConfigurationManager
{
    /// <summary>
    /// 전체 설정을 가져옵니다.
    /// </summary>
    /// <returns>AI 에이전트 설정</returns>
    AIAgentConfiguration GetConfiguration();
    
    /// <summary>
    /// 특정 섹션의 설정을 가져옵니다.
    /// </summary>
    /// <typeparam name="T">설정 타입</typeparam>
    /// <param name="sectionName">섹션 이름</param>
    /// <returns>섹션 설정</returns>
    T GetSection<T>(string sectionName) where T : class, new();
    
    /// <summary>
    /// 설정을 다시 로드합니다.
    /// </summary>
    void ReloadConfiguration();
    
    /// <summary>
    /// 설정의 유효성을 검증합니다.
    /// </summary>
    /// <returns>검증 성공 여부</returns>
    bool ValidateConfiguration();
    
    /// <summary>
    /// 연결 문자열을 가져옵니다.
    /// </summary>
    /// <param name="name">연결 문자열 이름</param>
    /// <returns>연결 문자열</returns>
    string GetConnectionString(string name);
    
    /// <summary>
    /// 설정 값을 설정합니다.
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    void SetValue(string key, object value);
}