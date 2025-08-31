using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace AIAgentFramework.Tools.Plugin;

/// <summary>
/// 플러그인 로더 구현
/// </summary>
public class PluginLoader : IPluginLoader, IDisposable
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly ConcurrentDictionary<string, PluginContext> _loadedPlugins;
    private readonly ConcurrentDictionary<string, AssemblyLoadContext> _loadContexts;
    private bool _disposed;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loadedPlugins = new ConcurrentDictionary<string, PluginContext>();
        _loadContexts = new ConcurrentDictionary<string, AssemblyLoadContext>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IPluginTool>> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading plugin from path: {PluginPath}", pluginPath);

            // 매니페스트 파일 찾기
            var manifestPath = Path.Combine(pluginPath, "plugin.json");
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException($"Plugin manifest not found: {manifestPath}");
            }

            // 매니페스트 로드
            var manifest = await LoadManifestAsync(manifestPath, cancellationToken);

            // 플러그인 검증
            if (!await ValidatePluginAsync(manifest, cancellationToken))
            {
                throw new InvalidOperationException($"Plugin validation failed: {manifest.Id}");
            }

            // 어셈블리 로드
            var assemblyPath = Path.IsPathRooted(manifest.AssemblyPath) 
                ? manifest.AssemblyPath 
                : Path.Combine(pluginPath, manifest.AssemblyPath);

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Plugin assembly not found: {assemblyPath}");
            }

            // 격리된 로드 컨텍스트 생성
            var loadContext = new AssemblyLoadContext($"Plugin_{manifest.Id}_{Guid.NewGuid()}", true);
            _loadContexts.TryAdd(manifest.Id, loadContext);

            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            // 플러그인 도구 타입 검색
            var pluginTools = new List<IPluginTool>();
            var toolTypes = assembly.GetTypes()
                .Where(t => typeof(IPluginTool).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var toolType in toolTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(toolType) as IPluginTool;
                    if (instance != null)
                    {
                        // 플러그인 초기화
                        await instance.InitializeAsync(manifest.DefaultConfiguration, cancellationToken);
                        pluginTools.Add(instance);
                        
                        _logger.LogDebug("Loaded plugin tool: {ToolName} from {PluginId}", 
                            instance.Name, manifest.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create instance of plugin tool: {ToolType}", toolType.Name);
                }
            }

            // 플러그인 컨텍스트 저장
            var pluginContext = new PluginContext
            {
                Manifest = manifest,
                LoadContext = loadContext,
                Tools = pluginTools,
                LoadedAt = DateTime.UtcNow
            };

            _loadedPlugins.TryAdd(manifest.Id, pluginContext);

            _logger.LogInformation("Successfully loaded plugin: {PluginId} with {ToolCount} tools", 
                manifest.Id, pluginTools.Count);

            return pluginTools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from path: {PluginPath}", pluginPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PluginManifest> LoadManifestAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Loading plugin manifest from: {ManifestPath}", manifestPath);

            var jsonContent = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<PluginManifest>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

            if (manifest == null)
            {
                throw new InvalidOperationException("Failed to deserialize plugin manifest");
            }

            // 기본값 설정
            if (string.IsNullOrWhiteSpace(manifest.Id))
            {
                manifest.Id = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(manifestPath)) ?? Guid.NewGuid().ToString();
            }

            _logger.LogDebug("Loaded plugin manifest: {PluginId} v{Version}", manifest.Id, manifest.Version);

            return manifest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin manifest from: {ManifestPath}", manifestPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unloading plugin: {PluginId}", pluginId);

            if (!_loadedPlugins.TryRemove(pluginId, out var pluginContext))
            {
                _logger.LogWarning("Plugin not found for unloading: {PluginId}", pluginId);
                return false;
            }

            // 플러그인 도구들 정리
            foreach (var tool in pluginContext.Tools)
            {
                try
                {
                    await tool.DisposeAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing plugin tool: {ToolName}", tool.Name);
                }
            }

            // 로드 컨텍스트 언로드
            if (_loadContexts.TryRemove(pluginId, out var loadContext))
            {
                loadContext.Unload();
            }

            _logger.LogInformation("Successfully unloaded plugin: {PluginId}", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin: {PluginId}", pluginId);
            return false;
        }
    }

    /// <inheritdoc />
    public IEnumerable<PluginManifest> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.Select(p => p.Manifest).ToList();
    }

    /// <inheritdoc />
    public Task<bool> ValidatePluginAsync(PluginManifest manifest, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating plugin: {PluginId}", manifest.Id);

            // 기본 필드 검증
            if (string.IsNullOrWhiteSpace(manifest.Id) ||
                string.IsNullOrWhiteSpace(manifest.Name) ||
                string.IsNullOrWhiteSpace(manifest.Version) ||
                string.IsNullOrWhiteSpace(manifest.AssemblyPath))
            {
                _logger.LogError("Plugin manifest has missing required fields: {PluginId}", manifest.Id);
                return Task.FromResult(false);
            }

            // 버전 형식 검증
            if (!Version.TryParse(manifest.Version, out _))
            {
                _logger.LogError("Invalid version format in plugin manifest: {Version}", manifest.Version);
                return Task.FromResult(false);
            }

            // 프레임워크 버전 호환성 검증
            if (!string.IsNullOrWhiteSpace(manifest.MinFrameworkVersion))
            {
                if (Version.TryParse(manifest.MinFrameworkVersion, out var minVersion))
                {
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    if (currentVersion != null && currentVersion < minVersion)
                    {
                        _logger.LogError("Plugin requires minimum framework version {MinVersion}, current is {CurrentVersion}", 
                            minVersion, currentVersion);
                        return Task.FromResult(false);
                    }
                }
            }

            // 의존성 검증 (기본적인 검증만 수행)
            foreach (var dependency in manifest.Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency.Name) || string.IsNullOrWhiteSpace(dependency.Version))
                {
                    _logger.LogError("Invalid dependency in plugin manifest: {DependencyName}", dependency.Name);
                    return Task.FromResult(false);
                }
            }

            _logger.LogDebug("Plugin validation successful: {PluginId}", manifest.Id);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating plugin: {PluginId}", manifest.Id);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // 모든 플러그인 언로드
            var pluginIds = _loadedPlugins.Keys.ToList();
            foreach (var pluginId in pluginIds)
            {
                try
                {
                    UnloadPluginAsync(pluginId).Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error unloading plugin during disposal: {PluginId}", pluginId);
                }
            }

            // 남은 로드 컨텍스트 정리
            foreach (var loadContext in _loadContexts.Values)
            {
                try
                {
                    loadContext.Unload();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error unloading assembly load context during disposal");
                }
            }

            _loadedPlugins.Clear();
            _loadContexts.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during plugin loader disposal");
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// 플러그인 컨텍스트
    /// </summary>
    private class PluginContext
    {
        public PluginManifest Manifest { get; set; } = null!;
        public AssemblyLoadContext LoadContext { get; set; } = null!;
        public List<IPluginTool> Tools { get; set; } = new();
        public DateTime LoadedAt { get; set; }
    }
}