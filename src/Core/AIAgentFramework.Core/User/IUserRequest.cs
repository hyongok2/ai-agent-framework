namespace AIAgentFramework.Core.User;

/// <summary>
/// 사용자 요청 인터페이스
/// </summary>
public interface IUserRequest
{
    /// <summary>
    /// 요청 ID
    /// </summary>
    string RequestId { get; }
    
    /// <summary>
    /// 사용자 ID
    /// </summary>
    string UserId { get; }
    
    /// <summary>
    /// 요청 내용
    /// </summary>
    string Content { get; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// 요청 시간
    /// </summary>
    DateTime RequestedAt { get; }
}