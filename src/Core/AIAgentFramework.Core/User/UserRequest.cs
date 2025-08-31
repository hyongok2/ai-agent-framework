namespace AIAgentFramework.Core.User;

/// <summary>
/// 사용자 요청 구현
/// </summary>
public class UserRequest : IUserRequest
{
    /// <inheritdoc />
    public string RequestId { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string UserId { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <inheritdoc />
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}