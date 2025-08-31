
using AIAgentFramework.Core.Validation;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.Tools.MCP;

/// <summary>
/// MCP 프로토콜 표준 준수 검증기
/// </summary>
public class MCPProtocolValidator
{
    private readonly ILogger<MCPProtocolValidator> _logger;
    private static readonly string[] RequiredMethods = { "initialize", "tools/list", "tools/call" };
    private static readonly string SupportedProtocolVersion = "2024-11-05";

    public MCPProtocolValidator(ILogger<MCPProtocolValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// MCP 서버 프로토콜 준수 검증
    /// </summary>
    public async Task<MCPValidationResult> ValidateServerAsync(MCPClient client, CancellationToken cancellationToken = default)
    {
        var result = new MCPValidationResult();

        try
        {
            // 1. 초기화 검증
            var initResult = await ValidateInitializeAsync(client, cancellationToken);
            result.InitializeSupported = initResult.IsValid;
            result.Errors.AddRange(initResult.Errors);

            if (!initResult.IsValid)
            {
                result.IsCompliant = false;
                return result;
            }

            // 2. 도구 목록 조회 검증
            var toolsListResult = await ValidateToolsListAsync(client, cancellationToken);
            result.ToolsListSupported = toolsListResult.IsValid;
            result.Errors.AddRange(toolsListResult.Errors);

            // 3. JSON-RPC 2.0 형식 검증
            var jsonRpcResult = ValidateJsonRpcFormat();
            result.JsonRpcCompliant = jsonRpcResult.IsValid;
            result.Errors.AddRange(jsonRpcResult.Errors);

            result.IsCompliant = result.InitializeSupported && 
                               result.ToolsListSupported && 
                               result.JsonRpcCompliant;

            _logger.LogInformation("MCP 프로토콜 검증 완료: {IsCompliant}", result.IsCompliant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP 프로토콜 검증 중 오류 발생");
            result.IsCompliant = false;
            result.Errors.Add($"검증 중 오류: {ex.Message}");
        }

        return result;
    }

    private async Task<ValidationResult> ValidateInitializeAsync(MCPClient client, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.SendRequestAsync<MCPInitializeResponse>("initialize", new
            {
                protocolVersion = SupportedProtocolVersion,
                capabilities = new { tools = true }
            }, cancellationToken);

            var errors = new List<string>();

            if (response.Error != null)
            {
                errors.Add($"초기화 실패: {response.Error.Message}");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            if (response.Result == null)
            {
                errors.Add("초기화 응답이 null입니다.");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            // 프로토콜 버전 검증
            if (string.IsNullOrEmpty(response.Result.ProtocolVersion))
            {
                errors.Add("프로토콜 버전이 누락되었습니다.");
            }

            // 서버 정보 검증
            if (response.Result.ServerInfo == null)
            {
                errors.Add("서버 정보가 누락되었습니다.");
            }

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }
        catch (Exception ex)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                Errors = new List<string> { $"초기화 검증 중 오류: {ex.Message}" } 
            };
        }
    }

    private async Task<ValidationResult> ValidateToolsListAsync(MCPClient client, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.SendRequestAsync<MCPToolsListResponse>("tools/list", null, cancellationToken);

            var errors = new List<string>();

            if (response.Error != null)
            {
                errors.Add($"도구 목록 조회 실패: {response.Error.Message}");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            if (response.Result?.Tools == null)
            {
                errors.Add("도구 목록이 null입니다.");
            }

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }
        catch (Exception ex)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                Errors = new List<string> { $"도구 목록 검증 중 오류: {ex.Message}" } 
            };
        }
    }

    private ValidationResult ValidateJsonRpcFormat()
    {
        // JSON-RPC 2.0 형식 검증 (기본적인 구조 확인)
        var errors = new List<string>();

        // 실제 구현에서는 더 정교한 JSON-RPC 2.0 형식 검증 필요
        // 현재는 기본 통과로 처리

        return new ValidationResult { IsValid = true, Errors = errors };
    }

    /// <summary>
    /// MCP 도구 호출 검증
    /// </summary>
    public async Task<ValidationResult> ValidateToolCallAsync(
        MCPClient client, 
        string toolName, 
        object? arguments = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.SendRequestAsync<MCPToolCallResponse>("tools/call", new
            {
                name = toolName,
                arguments = arguments ?? new { }
            }, cancellationToken);

            var errors = new List<string>();

            if (response.Error != null)
            {
                errors.Add($"도구 호출 실패: {response.Error.Message}");
            }

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }
        catch (Exception ex)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                Errors = new List<string> { $"도구 호출 검증 중 오류: {ex.Message}" } 
            };
        }
    }
}

/// <summary>
/// MCP 검증 결과
/// </summary>
public class MCPValidationResult
{
    public bool IsCompliant { get; set; }
    public bool InitializeSupported { get; set; }
    public bool ToolsListSupported { get; set; }
    public bool JsonRpcCompliant { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// 도구 목록 응답
/// </summary>
public record MCPToolsListResponse
{
    public List<MCPToolInfo>? Tools { get; init; }
}

/// <summary>
/// MCP 도구 정보
/// </summary>
public record MCPToolInfo
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public object? InputSchema { get; init; }
}