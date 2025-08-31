

using AIAgentFramework.Core.Tools.Abstractions;

namespace AIAgentFramework.Tools.MCP;

/// <summary>
/// MCP (Model Context Protocol) 도구 인터페이스
/// </summary>
public interface IMCPTool : ITool
{
    /// <summary>
    /// MCP 서버 연결 정보
    /// </summary>
    MCPConnectionInfo ConnectionInfo { get; }

    /// <summary>
    /// MCP 도구 메타데이터
    /// </summary>
    MCPToolMetadata Metadata { get; }

    /// <summary>
    /// MCP 서버와의 연결 상태
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// MCP 서버에 연결
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// MCP 서버와의 연결 해제
    /// </summary>
    Task DisconnectAsync();
}

/// <summary>
/// MCP 연결 정보
/// </summary>
public record MCPConnectionInfo
{
    public required string ServerUrl { get; init; }
    public required MCPTransportType TransportType { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// MCP 전송 방식
/// </summary>
public enum MCPTransportType
{
    Http,
    WebSocket,
    Stdio
}

/// <summary>
/// MCP 도구 메타데이터
/// </summary>
public record MCPToolMetadata
{
    public required string ServerName { get; init; }
    public required string ServerVersion { get; init; }
    public required string ProtocolVersion { get; init; }
    public List<string> SupportedCapabilities { get; init; } = new();
}