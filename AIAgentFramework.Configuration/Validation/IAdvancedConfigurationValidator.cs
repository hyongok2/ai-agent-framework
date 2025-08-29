using AIAgentFramework.Configuration.Models;

namespace AIAgentFramework.Configuration.Validation;

/// <summary>
/// 고급 설정 검증기 인터페이스
/// </summary>
public interface IAdvancedConfigurationValidator
{
    /// <summary>
    /// JSON Schema를 사용하여 설정을 검증합니다.
    /// </summary>
    /// <param name="configuration">검증할 설정</param>
    /// <param name="schemaPath">JSON Schema 파일 경로</param>
    /// <returns>검증 결과</returns>
    ValidationResult ValidateWithSchema(AIAgentConfiguration configuration, string schemaPath);
    
    /// <summary>
    /// 환경별 특화 검증을 수행합니다.
    /// </summary>
    /// <param name="configuration">검증할 설정</param>
    /// <returns>검증 결과</returns>
    ValidationResult ValidateEnvironmentSpecific(AIAgentConfiguration configuration);
    
    /// <summary>
    /// 외부 서비스 연결성을 검증합니다.
    /// </summary>
    /// <param name="configuration">검증할 설정</param>
    /// <returns>검증 결과</returns>
    ValidationResult ValidateConnectivity(AIAgentConfiguration configuration);
    
    /// <summary>
    /// 성능 관련 설정을 검증합니다.
    /// </summary>
    /// <param name="configuration">검증할 설정</param>
    /// <returns>검증 결과</returns>
    ValidationResult ValidatePerformanceSettings(AIAgentConfiguration configuration);
    
    /// <summary>
    /// 보안 관련 설정을 검증합니다.
    /// </summary>
    /// <param name="configuration">검증할 설정</param>
    /// <returns>검증 결과</returns>
    ValidationResult ValidateSecuritySettings(AIAgentConfiguration configuration);
    
    /// <summary>
    /// 사용자 정의 검증기를 등록합니다.
    /// </summary>
    /// <param name="name">검증기 이름</param>
    /// <param name="validator">검증 함수</param>
    void RegisterCustomValidator(string name, Func<object, ValidationResult> validator);
    
    /// <summary>
    /// 사용자 정의 검증을 실행합니다.
    /// </summary>
    /// <param name="name">검증기 이름</param>
    /// <param name="target">검증 대상</param>
    /// <returns>검증 결과</returns>
    ValidationResult RunCustomValidation(string name, object target);
}