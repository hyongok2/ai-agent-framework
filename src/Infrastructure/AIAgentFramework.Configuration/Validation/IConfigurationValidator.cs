using AIAgentFramework.Configuration.Models;

namespace AIAgentFramework.Configuration.Validation;

/// <summary>
/// 설정 검증기 인터페이스
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// 설정의 유효성을 검증합니다.
    /// </summary>
    /// <param name="configuration">검증할 설정</param>
    /// <returns>검증 결과</returns>
    ValidationResult ValidateConfiguration(AIAgentConfiguration configuration);
}