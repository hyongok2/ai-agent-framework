using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Tools.MCP;

/// <summary>
/// MCP 클라이언트 (JSON-RPC 2.0 기반)
/// </summary>
public class MCPClient : IDisposable
{
    private readonly HttpClient? _httpClient;
    private readonly ClientWebSocket? _webSocket;
    private readonly ILogger _logger;
    private readonly MCPConnectionInfo _connectionInfo;
    private int _requestId = 0;
    private bool _disposed = false;

    public MCPClient(MCPConnectionInfo connectionInfo, ILogger logger)
    {
        _connectionInfo = connectionInfo;
        _logger = logger;
        
        if (connectionInfo.TransportType == MCPTransportType.Http)
        {
            _httpClient = new HttpClient { Timeout = connectionInfo.Timeout };
            foreach (var header in connectionInfo.Headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
        else if (connectionInfo.TransportType == MCPTransportType.WebSocket)
        {
            _webSocket = new ClientWebSocket();
            foreach (var header in connectionInfo.Headers)
            {
                _webSocket.Options.SetRequestHeader(header.Key, header.Value);
            }
        }
    }

    /// <summary>
    /// MCP 서버에 연결
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connectionInfo.TransportType == MCPTransportType.WebSocket && _webSocket != null)
            {
                await _webSocket.ConnectAsync(new Uri(_connectionInfo.ServerUrl), cancellationToken);
                return _webSocket.State == WebSocketState.Open;
            }
            return true; // HTTP는 연결 상태 없음
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP 서버 연결 실패: {ServerUrl}", _connectionInfo.ServerUrl);
            return false;
        }
    }

    /// <summary>
    /// JSON-RPC 2.0 요청 전송
    /// </summary>
    public async Task<MCPResponse<T>> SendRequestAsync<T>(string method, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var request = new MCPRequest
        {
            Id = Interlocked.Increment(ref _requestId),
            Method = method,
            Params = parameters
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        
        if (_connectionInfo.TransportType == MCPTransportType.Http)
        {
            return await SendHttpRequestAsync<T>(json, cancellationToken);
        }
        else if (_connectionInfo.TransportType == MCPTransportType.WebSocket)
        {
            return await SendWebSocketRequestAsync<T>(json, cancellationToken);
        }
        
        throw new NotSupportedException($"전송 방식 {_connectionInfo.TransportType}은 지원되지 않습니다.");
    }

    private async Task<MCPResponse<T>> SendHttpRequestAsync<T>(string json, CancellationToken cancellationToken)
    {
        if (_httpClient == null)
            throw new InvalidOperationException("HTTP 클라이언트가 초기화되지 않았습니다.");
            
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_connectionInfo.ServerUrl, content, cancellationToken);
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<MCPResponse<T>>(responseJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
    }

    private async Task<MCPResponse<T>> SendWebSocketRequestAsync<T>(string json, CancellationToken cancellationToken)
    {
        if (_webSocket?.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket 연결이 열려있지 않습니다.");

        var buffer = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);

        var responseBuffer = new byte[4096];
        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
        
        var responseJson = Encoding.UTF8.GetString(responseBuffer, 0, result.Count);
        return JsonSerializer.Deserialize<MCPResponse<T>>(responseJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _webSocket?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// JSON-RPC 2.0 요청
/// </summary>
public record MCPRequest
{
    public string Jsonrpc { get; init; } = "2.0";
    public required int Id { get; init; }
    public required string Method { get; init; }
    public object? Params { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 응답
/// </summary>
public record MCPResponse<T>
{
    public string Jsonrpc { get; init; } = "2.0";
    public int Id { get; init; }
    public T? Result { get; init; }
    public MCPError? Error { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 오류
/// </summary>
public record MCPError
{
    public int Code { get; init; }
    public required string Message { get; init; }
    public object? Data { get; init; }
}