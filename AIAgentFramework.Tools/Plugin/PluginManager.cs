using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AIAgentFramework.Tools.Plugin;

/// <summary>
/// 플러그인 관리자 구현
/// </summary>
public class PluginManager : IPluginManager, IDisposable
{
    private readonly IPluginLoader _pluginLoader;
    private readonly ILogger<PluginManager> _logger;
    private readonly ConcurrentDictionary<string, IPluginTool> _pluginTools;
    private readonly ConcurrentDictionary<string, Dictionary<string, object>> _pluginConfigurations;
    private bool _disposed;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="pluginLoader">플러그인 로더</param>
    /// <param name="logger">로거</param>
    public PluginManager(IPluginLoader pluginLoader, ILogger<PluginManager> logger)
    {
        _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pluginTools = new ConcurrentDictionary<string, IPluginTool>();
        _pluginConfigurations = new ConcurrentDictionary<string, Dictionary<string, object>>();
    }

    /// <inheritdoc />
    public async Task<int> LoadPluginsFromDirectoryAsync(string pluginDirectory, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading plugins from directory: {PluginDirectory}", pluginDirectory);

            if (!Directory.Exists(pluginDirectory))
            {
                _logger.LogWarning("Plugin directory does not exist: {PluginDirectory}", pluginDirectory);
                return 0;
            }

            var loadedCount = 0;
            var pluginDirectories = Directory.GetDirectories(pluginDirectory);

            foreach (var pluginPath in pluginDirectories)
            {
                try
                {
                    var tools = await LoadPluginAsync(pluginPath, cancellationToken);
                    loadedCount += tools.Count();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin from directory: {PluginPath}", pluginPath);
                }
            }

            _logger.LogInformation("Loaded {LoadedCount} plugin tools from {DirectoryCount} directories", 
                loadedCount, pluginDirectories.Length);

            return loadedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugins from directory: {PluginDirectory}", pluginDirectory);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IPluginTool>> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading plugin from path: {PluginPath}", pluginPath);

            var tools = await _pluginLoader.LoadPluginAsync(pluginPath, cancellationToken);
            var loadedTools = new List<IPluginTool>();

            foreach (var tool in tools)
            {
                // 도구 이름 중복 확인
                if (_pluginTools.ContainsKey(tool.Name))
                {
                    _logger.LogWarning("Plugin tool with name '{ToolName}' already exists, skipping", tool.Name);
                    continue;
                }

                // 도구 등록
                if (_pluginTools.TryAdd(tool.Name, tool))
                {
                    loadedTools.Add(tool);
                    _logger.LogDebug("Registered plugin tool: {ToolName}", tool.Name);
                }
            }

            _logger.LogInformation("Successfully loaded {ToolCount} plugin tools from: {PluginPath}", 
                loadedTools.Count, pluginPath);

            return loadedTools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from path: {PluginPath}", pluginPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unloading plugin: {PluginId}", pluginId);

            // 플러그인에 속한 도구들 찾기 및 제거
            var pluginManifests = _pluginLoader.GetLoadedPlugins();
            var targetManifest = pluginManifests.FirstOrDefault(m => m.Id == pluginId);
            
            if (targetManifest == null)
            {
                _logger.LogWarning("Plugin not found: {PluginId}", pluginId);
                return false;
            }

            // 해당 플러그인의 도구들을 찾아서 제거
            var toolsToRemove = _pluginTools.Values
                .Where(tool => tool is IPluginTool pluginTool && 
                              GetPluginIdForTool(pluginTool) == pluginId)
                .ToList();

            foreach (var tool in toolsToRemove)
            {
                if (_pluginTools.TryRemove(tool.Name, out _))
                {
                    _logger.LogDebug("Removed plugin tool: {ToolName}", tool.Name);
                }
            }

            // 플러그인 설정 제거
            _pluginConfigurations.TryRemove(pluginId, out _);

            // 플러그인 로더에서 언로드
            var result = await _pluginLoader.UnloadPluginAsync(pluginId, cancellationToken);

            _logger.LogInformation("Plugin unload result for {PluginId}: {Result}", pluginId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin: {PluginId}", pluginId);
            return false;
        }
    }

    /// <inheritdoc />
    public IPluginTool? GetPluginTool(string toolName)
    {
        _pluginTools.TryGetValue(toolName, out var tool);
        return tool;
    }

    /// <inheritdoc />
    public IEnumerable<IPluginTool> GetAllPluginTools()
    {
        return _pluginTools.Values.ToList();
    }

    /// <inheritdoc />
    public IEnumerable<PluginManifest> GetLoadedPlugins()
    {
        return _pluginLoader.GetLoadedPlugins();
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePluginConfigurationAsync(string pluginId, Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating configuration for plugin: {PluginId}", pluginId);

            // 설정 저장
            _pluginConfigurations.AddOrUpdate(pluginId, configuration, (key, oldValue) => configuration);

            // 해당 플러그인의 도구들에 새 설정 적용
            var pluginTools = _pluginTools.Values
                .Where(tool => tool is IPluginTool pluginTool && 
                              GetPluginIdForTool(pluginTool) == pluginId)
                .Cast<IPluginTool>()
                .ToList();

            var updateTasks = pluginTools.Select(async tool =>
            {
                try
                {
                    await tool.InitializeAsync(configuration, cancellationToken);
                    _logger.LogDebug("Updated configuration for tool: {ToolName}", tool.Name);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update configuration for tool: {ToolName}", tool.Name);
                    return false;
                }
            });

            var results = await Task.WhenAll(updateTasks);
            var successCount = results.Count(r => r);

            _logger.LogInformation("Updated configuration for {SuccessCount}/{TotalCount} tools in plugin: {PluginId}", 
                successCount, pluginTools.Count, pluginId);

            return successCount == pluginTools.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update plugin configuration: {PluginId}", pluginId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>?> GetPluginStatusAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting status for plugin: {PluginId}", pluginId);

            var pluginTools = _pluginTools.Values
                .Where(tool => tool is IPluginTool pluginTool && 
                              GetPluginIdForTool(pluginTool) == pluginId)
                .Cast<IPluginTool>()
                .ToList();

            if (!pluginTools.Any())
            {
                _logger.LogWarning("No tools found for plugin: {PluginId}", pluginId);
                return null;
            }

            var status = new Dictionary<string, object>
            {
                ["plugin_id"] = pluginId,
                ["tool_count"] = pluginTools.Count,
                ["tools"] = new List<Dictionary<string, object>>()
            };

            var toolStatuses = new List<Dictionary<string, object>>();

            foreach (var tool in pluginTools)
            {
                try
                {
                    var toolStatus = await tool.GetStatusAsync();
                    toolStatus["tool_name"] = tool.Name;
                    toolStatuses.Add(toolStatus);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get status for tool: {ToolName}", tool.Name);
                    toolStatuses.Add(new Dictionary<string, object>
                    {
                        ["tool_name"] = tool.Name,
                        ["error"] = ex.Message
                    });
                }
            }

            status["tools"] = toolStatuses;

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get plugin status: {PluginId}", pluginId);
            return null;
        }
    }

    /// <summary>
    /// 도구에서 플러그인 ID 추출
    /// </summary>
    /// <param name="tool">플러그인 도구</param>
    /// <returns>플러그인 ID</returns>
    private string GetPluginIdForTool(IPluginTool tool)
    {
        // 실제 구현에서는 도구에서 플러그인 ID를 가져오는 로직이 필요
        // 여기서는 간단히 어셈블리 이름을 사용
        return tool.GetType().Assembly.GetName().Name ?? "unknown";
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // 모든 플러그인 도구 정리
            var disposeTasks = _pluginTools.Values
                .Cast<IPluginTool>()
                .Select(async tool =>
                {
                    try
                    {
                        await tool.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing plugin tool: {ToolName}", tool.Name);
                    }
                });

            Task.WaitAll(disposeTasks.ToArray(), TimeSpan.FromSeconds(10));

            _pluginTools.Clear();
            _pluginConfigurations.Clear();

            // 플러그인 로더 정리
            if (_pluginLoader is IDisposable disposableLoader)
            {
                disposableLoader.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during plugin manager disposal");
        }
        finally
        {
            _disposed = true;
        }
    }
}