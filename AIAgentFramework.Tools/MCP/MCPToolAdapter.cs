using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.Tools.MCP;

/// <summary>
/// MCP 도구 어댑터
/// </summary>
public class MCPToolAdapter : IMCPTool
{
    private readonly MCPClient _client;
    private readonly ILogger _logger;
    private MCPToolMetadata? _metadata;

    public string Name { get; }
    public string Description { get; }
    public IToolContract Contract { get; }
    public MCPConnectionInfo ConnectionInfo { get; }
    public MCPToolMetadata Metadata => _metadata ?? throw new InvalidOperationException("연결되지 않은 상태입니다.");
    public bool IsConnected { get; private set; }

    public MCPToolAdapter(
        string name,
        string description,
        IToolContract contract,
        MCPConnectionInfo connectionInfo,
        ILogger logger)
    {
        Name = name;
        Description = description;
        Contract = contract;
        ConnectionInfo = connectionInfo;
        _logger = logger;
        _client = new MCPClient(connectionInfo, logger);
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connected = await _client.ConnectAsync(cancellationToken);
            if (!connected) return false;

            // MCP 서버 정보 조회
            var initResponse = await _client.SendRequestAsync<MCPInitializeResponse>("initialize", new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = true }
            }, cancellationToken);

            if (initResponse.Error != null)
            {
                _logger.LogError("MCP 초기화 실패: {Error}", initResponse.Error.Message);
                return false;
            }

            _metadata = new MCPToolMetadata
            {
                ServerName = initResponse.Result?.ServerInfo?.Name ?? "Unknown",
                ServerVersion = initResponse.Result?.ServerInfo?.Version ?? "Unknown",
                ProtocolVersion = initResponse.Result?.ProtocolVersion ?? "Unknown",
                SupportedCapabilities = initResponse.Result?.Capabilities?.Tools?.ListChanged == true 
                    ? new List<string> { "tools" } : new List<string>()
            };

            IsConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP 연결 실패");
            return false;
        }
    }

    public Task DisconnectAsync()
    {
        _client.Dispose();
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<bool> ValidateAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        var validationResult = Contract.ValidateInput(input);
        return Task.FromResult(validationResult.IsValid);
    }

    public async Task<IToolResult> ExecuteAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("MCP 서버에 연결되지 않았습니다.");

        try
        {
            // 입력 검증
            var validationResult = Contract.ValidateInput(input);
            if (!validationResult.IsValid)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", validationResult.Errors)
                };
            }

            // MCP 도구 호출
            var response = await _client.SendRequestAsync<MCPToolCallResponse>("tools/call", new
            {
                name = Name,
                arguments = input.Parameters
            }, cancellationToken);

            if (response.Error != null)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = response.Error.Message
                }.WithData("error_data", response.Error.Data ?? new object());
            }

            var result = new ToolResult
            {
                Success = true
            }
            .WithData("content", response.Result?.Content ?? new object())
            .WithMetadata("mcp_tool_name", Name)
            .WithMetadata("mcp_server", Metadata.ServerName);

            // 출력 검증
            var outputValidation = Contract.ValidateOutput(result);
            if (!outputValidation.IsValid)
            {
                _logger.LogWarning("MCP 도구 출력 검증 실패: {Errors}", string.Join(", ", outputValidation.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP 도구 실행 중 오류 발생");
            return new ToolResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// MCP 초기화 응답
/// </summary>
public record MCPInitializeResponse
{
    public string? ProtocolVersion { get; init; }
    public MCPServerInfo? ServerInfo { get; init; }
    public MCPCapabilities? Capabilities { get; init; }
}

/// <summary>
/// MCP 서버 정보
/// </summary>
public record MCPServerInfo
{
    public string? Name { get; init; }
    public string? Version { get; init; }
}

/// <summary>
/// MCP 기능
/// </summary>
public record MCPCapabilities
{
    public MCPToolsCapability? Tools { get; init; }
}

/// <summary>
/// MCP 도구 기능
/// </summary>
public record MCPToolsCapability
{
    public bool ListChanged { get; init; }
}

/// <summary>
/// MCP 도구 호출 응답
/// </summary>
public record MCPToolCallResponse
{
    public object? Content { get; init; }
    public bool IsError { get; init; }
}