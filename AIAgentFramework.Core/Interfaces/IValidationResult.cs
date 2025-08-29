namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 검증 결과 인터페이스
/// </summary>
public interface IValidationResult
{
    /// <summary>
    /// 검증 성공 여부
    /// </summary>
    bool IsValid { get; }
    
    /// <summary>
    /// 오류 메시지 목록
    /// </summary>
    List<string> Errors { get; }
    
    /// <summary>
    /// 경고 메시지 목록
    /// </summary>
    List<string> Warnings { get; }
    
    /// <summary>
    /// 검증 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }
}