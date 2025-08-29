using AIAgentFramework.Configuration.Models;

namespace AIAgentFramework.Configuration.Templates;

/// <summary>
/// 설정 템플릿 생성기 인터페이스
/// </summary>
public interface IConfigurationTemplateGenerator
{
    /// <summary>
    /// YAML 형식의 설정 템플릿을 생성합니다.
    /// </summary>
    /// <param name="environment">환경 이름</param>
    /// <returns>YAML 템플릿 문자열</returns>
    string GenerateYamlTemplate(string environment = "Development");
    
    /// <summary>
    /// JSON 형식의 설정 템플릿을 생성합니다.
    /// </summary>
    /// <param name="environment">환경 이름</param>
    /// <returns>JSON 템플릿 문자열</returns>
    string GenerateJsonTemplate(string environment = "Development");
    
    /// <summary>
    /// 기본 설정 객체를 생성합니다.
    /// </summary>
    /// <param name="environment">환경 이름</param>
    /// <returns>기본 설정 객체</returns>
    AIAgentConfiguration CreateDefaultConfiguration(string environment = "Development");
    
    /// <summary>
    /// 템플릿을 파일로 저장합니다.
    /// </summary>
    /// <param name="filePath">저장할 파일 경로</param>
    /// <param name="content">템플릿 내용</param>
    void SaveTemplate(string filePath, string content);
    
    /// <summary>
    /// 템플릿의 유효성을 검증합니다.
    /// </summary>
    /// <param name="templateContent">템플릿 내용</param>
    /// <param name="format">템플릿 형식 (yaml, json)</param>
    /// <returns>검증 성공 여부</returns>
    bool ValidateTemplate(string templateContent, string format = "yaml");
}