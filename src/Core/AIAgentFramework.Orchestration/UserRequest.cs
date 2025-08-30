using AIAgentFramework.Core.Interfaces;

namespace AIAgentFramework.Orchestration;

/// <summary>
/// 사용자 요청 구현
/// </summary>
public class UserRequest : IUserRequest
{
    /// <inheritdoc />
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    
    /// <inheritdoc />
    public string UserId { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <inheritdoc />
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <inheritdoc />
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 생성자
    /// </summary>
    public UserRequest()
    {
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="content">요청 내용</param>
    public UserRequest(string content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}