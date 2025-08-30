using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 도구 계약 인터페이스
/// </summary>
public interface IToolContract
{
    /// <summary>
    /// 입력 스키마 (JSON Schema)
    /// </summary>
    string InputSchema { get; }
    
    /// <summary>
    /// 출력 스키마 (JSON Schema)
    /// </summary>
    string OutputSchema { get; }
    
    /// <summary>
    /// 필수 파라미터 목록
    /// </summary>
    List<string> RequiredParameters { get; }
    
    /// <summary>
    /// 선택적 파라미터 목록
    /// </summary>
    List<string> OptionalParameters { get; }
    
    /// <summary>
    /// 입력 검증
    /// </summary>
    ValidationResult ValidateInput(IToolInput input);
    
    /// <summary>
    /// 출력 검증
    /// </summary>
    ValidationResult ValidateOutput(IToolResult output);
}