using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Tools.Extensions;
using AIAgentFramework.Tools.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PluginTestApp;

class Program
{
    static async Task Main(string[] args)
    {
        // 서비스 컨테이너 설정
        var services = new ServiceCollection();
        
        // 로깅 설정
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        // Tools 시스템 등록
        services.AddAllTools();
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Plugin Test App Started");
        
        try
        {
            // 플러그인 매니저 가져오기
            var pluginManager = serviceProvider.GetRequiredService<IPluginManager>();
            
            // 샘플 플러그인 로드 (빌드된 플러그인 경로)
            var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SampleWebSearchPlugin");
            
            logger.LogInformation("Loading plugin from: {PluginPath}", pluginPath);
            
            var loadedTools = await pluginManager.LoadPluginAsync(pluginPath);
            
            logger.LogInformation("Loaded {Count} plugin tools", loadedTools.Count());
            
            // 로드된 플러그인 정보 출력
            var loadedPlugins = pluginManager.GetLoadedPlugins();
            foreach (var plugin in loadedPlugins)
            {
                logger.LogInformation("Plugin: {Name} v{Version} by {Author}", 
                    plugin.Name, plugin.Version, plugin.Author);
            }
            
            // 플러그인 도구 테스트
            var webSearchTool = pluginManager.GetPluginTool("WebSearch");
            if (webSearchTool != null)
            {
                logger.LogInformation("Testing WebSearch tool...");
                
                var input = new ToolInput()
                    .WithParameter("query", "AI Agent Framework")
                    .WithParameter("max_results", 3)
                    .WithParameter("language", "ko");
                
                var result = await webSearchTool.ExecuteAsync(input);
                
                logger.LogInformation("WebSearch result: Success={Success}", result.Success);
                
                if (result.Success && result.Data.ContainsKey("results"))
                {
                    var results = result.Data["results"] as List<Dictionary<string, object>>;
                    if (results != null)
                    {
                        foreach (var searchResult in results)
                        {
                            logger.LogInformation("  - {Title}: {Url}", 
                                searchResult.GetValueOrDefault("title", "No Title"),
                                searchResult.GetValueOrDefault("url", "No URL"));
                        }
                    }
                }
                
                // 플러그인 상태 확인
                var status = await webSearchTool.GetStatusAsync();
                logger.LogInformation("WebSearch tool status: {Status}", 
                    string.Join(", ", status.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            }
            else
            {
                logger.LogWarning("WebSearch tool not found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during plugin testing");
        }
        
        logger.LogInformation("Plugin Test App Completed");
        
        // 서비스 정리
        await serviceProvider.DisposeAsync();
    }
}