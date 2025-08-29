using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.Tools.MCP;

/// <summary>
/// MCP 도구 로더
/// </summary>
public class MCPToolLoader
{
    private readonly ILogger<MCPToolLoader> _logger;
    private readonly Dictionary<string, IMCPTool> _loadedTools = new();

    public MCPToolLoader(ILogger<MCPToolLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// MCP 설정 파일에서 도구 로드
    /// </summary>
    public async Task<IEnumerable<IMCPTool>> LoadFromConfigAsync(string configPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("MCP 설정 파일을 찾을 수 없습니다: {ConfigPath}", configPath);
                return Enumerable.Empty<IMCPTool>();
            }

            var json = await File.ReadAllTextAsync(configPath, cancellationToken);
            var config = JsonSerializer.Deserialize<MCPToolsConfig>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            if (config?.Tools == null)
            {
                _logger.LogWarning("MCP 설정 파일에 도구 정보가 없습니다: {ConfigPath}", configPath);
                return Enumerable.Empty<IMCPTool>();
            }

            var tools = new List<IMCPTool>();
            foreach (var toolConfig in config.Tools)
            {
                var tool = await CreateToolFromConfigAsync(toolConfig, cancellationToken);
                if (tool != null)
                {
                    tools.Add(tool);
                    _loadedTools[tool.Name] = tool;
                }
            }

            _logger.LogInformation("MCP 도구 {Count}개 로드 완료", tools.Count);
            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP 도구 로드 중 오류 발생: {ConfigPath}", configPath);
            return Enumerable.Empty<IMCPTool>();
        }
    }

    /// <summary>
    /// 단일 MCP 도구 생성
    /// </summary>
    public async Task<IMCPTool?> CreateToolAsync(
        string name,
        string description,
        MCPConnectionInfo connectionInfo,
        IToolContract contract,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tool = new MCPToolAdapter(name, description, contract, connectionInfo, logger);
            
            var connected = await tool.ConnectAsync(cancellationToken);
            if (!connected)
            {
                _logger.LogError("MCP 도구 연결 실패: {ToolName}", name);
                return null;
            }

            _loadedTools[name] = tool;
            return tool;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP 도구 생성 중 오류 발생: {ToolName}", name);
            return null;
        }
    }

    /// <summary>
    /// 로드된 도구 조회
    /// </summary>
    public IMCPTool? GetTool(string name)
    {
        return _loadedTools.TryGetValue(name, out var tool) ? tool : null;
    }

    /// <summary>
    /// 모든 로드된 도구 조회
    /// </summary>
    public IEnumerable<IMCPTool> GetAllTools()
    {
        return _loadedTools.Values;
    }

    /// <summary>
    /// 도구 연결 해제 및 정리
    /// </summary>
    public async Task UnloadAllAsync()
    {
        foreach (var tool in _loadedTools.Values)
        {
            try
            {
                await tool.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP 도구 연결 해제 중 오류: {ToolName}", tool.Name);
            }
        }
        _loadedTools.Clear();
    }

    private async Task<IMCPTool?> CreateToolFromConfigAsync(MCPToolConfig toolConfig, CancellationToken cancellationToken)
    {
        try
        {
            var connectionInfo = new MCPConnectionInfo
            {
                ServerUrl = toolConfig.ServerUrl,
                TransportType = Enum.Parse<MCPTransportType>(toolConfig.TransportType, true),
                Headers = toolConfig.Headers ?? new Dictionary<string, string>(),
                Timeout = TimeSpan.FromSeconds(toolConfig.TimeoutSeconds ?? 30)
            };

            var contract = CreateContractFromConfig(toolConfig);
            return await CreateToolAsync(toolConfig.Name, toolConfig.Description, connectionInfo, contract, _logger, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP 도구 설정 처리 중 오류: {ToolName}", toolConfig.Name);
            return null;
        }
    }

    private IToolContract CreateContractFromConfig(MCPToolConfig toolConfig)
    {
        // 기본 Contract 생성 (실제 구현에서는 더 정교한 스키마 처리 필요)
        return new BasicToolContract
        {
            InputSchema = toolConfig.InputSchema ?? "{}",
            OutputSchema = toolConfig.OutputSchema ?? "{}"
        };
    }
}

/// <summary>
/// MCP 도구 설정
/// </summary>
public record MCPToolsConfig
{
    public List<MCPToolConfig>? Tools { get; init; }
}

/// <summary>
/// 개별 MCP 도구 설정
/// </summary>
public record MCPToolConfig
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string ServerUrl { get; init; }
    public required string TransportType { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? InputSchema { get; init; }
    public string? OutputSchema { get; init; }
}

/// <summary>
/// 기본 도구 Contract 구현
/// </summary>
public class BasicToolContract : IToolContract
{
    public required string InputSchema { get; init; }
    public required string OutputSchema { get; init; }
    public List<string> RequiredParameters { get; init; } = new();
    public List<string> OptionalParameters { get; init; } = new();

    public ValidationResult ValidateInput(IToolInput input)
    {
        // 기본 검증 (실제로는 JSON Schema 검증 구현 필요)
        return new ValidationResult { IsValid = true, Errors = new List<string>() };
    }

    public ValidationResult ValidateOutput(IToolResult output)
    {
        // 기본 검증 (실제로는 JSON Schema 검증 구현 필요)
        return new ValidationResult { IsValid = true, Errors = new List<string>() };
    }
}