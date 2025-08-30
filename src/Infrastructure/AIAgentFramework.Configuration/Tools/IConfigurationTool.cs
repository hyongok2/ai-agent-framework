using AIAgentFramework.Configuration.Models;
using AIAgentFramework.Configuration.Validation;
using AIAgentFramework.Configuration.Tools;

namespace AIAgentFramework.Configuration.Tools;

/// <summary>
/// 설정 관리 도구 인터페이스
/// </summary>
public interface IConfigurationTool
{
    /// <summary>
    /// 환경별 설정 파일들을 생성합니다.
    /// </summary>
    /// <param name="outputDirectory">출력 디렉토리</param>
    /// <param name="environments">생성할 환경 목록</param>
    void GenerateConfigurationFiles(string outputDirectory, string[]? environments = null);
    
    /// <summary>
    /// 설정 파일의 유효성을 검증합니다.
    /// </summary>
    /// <param name="filePath">설정 파일 경로</param>
    /// <param name="includeAdvancedValidation">고급 검증 포함 여부</param>
    /// <returns>검증 결과</returns>
    ValidationResult ValidateConfigurationFile(string filePath, bool includeAdvancedValidation = true);
    
    /// <summary>
    /// 설정 파일을 다른 버전으로 마이그레이션합니다.
    /// </summary>
    /// <param name="sourceFilePath">원본 파일 경로</param>
    /// <param name="targetFilePath">대상 파일 경로</param>
    /// <param name="targetVersion">대상 버전</param>
    void MigrateConfiguration(string sourceFilePath, string targetFilePath, string targetVersion = "1.0.0");
    
    /// <summary>
    /// 두 설정 파일을 비교합니다.
    /// </summary>
    /// <param name="filePath1">첫 번째 파일 경로</param>
    /// <param name="filePath2">두 번째 파일 경로</param>
    /// <returns>비교 결과</returns>
    ConfigurationComparisonResult CompareConfigurations(string filePath1, string filePath2);
    
    /// <summary>
    /// 설정 문서를 내보냅니다.
    /// </summary>
    /// <param name="configuration">설정 객체</param>
    /// <param name="outputPath">출력 파일 경로</param>
    /// <param name="format">문서 형식 (markdown, html, json)</param>
    void ExportConfigurationDocumentation(AIAgentConfiguration configuration, string outputPath, string format = "markdown");
}