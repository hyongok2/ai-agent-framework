namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 도구 입력 인터페이스
/// </summary>
public interface IToolInput
{
    /// <summary>
    /// 파라미터
    /// </summary>
    Dictionary<string, object> Parameters { get; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// 컨텍스트 정보
    /// </summary>
    Dictionary<string, object> Context { get; }
}