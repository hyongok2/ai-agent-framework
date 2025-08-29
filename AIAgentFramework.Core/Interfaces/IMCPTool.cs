namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// MCP(Model Context Protocol) 도구 인터페이스
/// </summary>
public interface IMCPTool : ITool
{
    /// <summary>
    /// MCP 버전
    /// </summary>
    string MCPVersion { get; }
    
    /// <summary>
    /// 연결 엔드포인트
    /// </summary>
    string Endpoint { get; }
    
    /// <summary>
    /// 연결 상태
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// MCP 서버에 연결합니다.
    /// </summary>
    /// <param name="endpoint">연결 엔드포인트</param>
    /// <returns>연결 성공 여부</returns>
    Task<bool> ConnectAsync(string endpoint);
    
    /// <summary>
    /// MCP 서버와의 연결을 해제합니다.
    /// </summary>
    /// <returns>연결 해제 작업</returns>
    Task DisconnectAsync();
    
    /// <summary>
    /// 연결 상태를 확인합니다.
    /// </summary>
    /// <returns>연결 상태</returns>
    Task<bool> PingAsync();
}