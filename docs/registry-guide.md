# AI Agent Framework - Registry System Guide

## Overview

The Registry system is the central component management hub of the AI Agent Framework. It provides automatic discovery, registration, and lifecycle management for LLM functions and tools.

## Key Features

- **Automatic Component Discovery**: Scan assemblies for LLM functions and tools
- **Metadata Management**: Rich metadata support for components
- **Flexible Registration**: Manual and automatic registration options
- **Component Lifecycle**: Enable/disable components at runtime
- **Usage Statistics**: Track component usage and performance
- **Type-Safe Queries**: Find components by type, category, or tags
- **Dependency Injection Integration**: Seamless DI container integration

## Architecture

### Core Components

1. **IRegistry**: Basic registry interface
2. **IAdvancedRegistry**: Extended registry with advanced features
3. **Registry**: Main implementation class
4. **ComponentMetadata**: Metadata model for components
5. **ComponentDiscovery**: Utility for discovering components

### Component Types

- **LLM Functions**: AI language model functions (planner, interpreter, etc.)
- **Tools**: Executable tools (built-in, plugins, MCP tools)

## Basic Usage

### Service Registration

```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Basic registry registration
builder.Services.AddRegistry();

// With automatic assembly scanning
builder.Services.AddRegistryWithAutoRegistration(
    Assembly.GetExecutingAssembly(),
    Assembly.LoadFrom("MyPlugins.dll")
);

// With current assembly
builder.Services.AddRegistryWithCurrentAssembly();

// With namespace pattern
builder.Services.AddRegistryWithNamespacePattern(
    Assembly.GetExecutingAssembly(),
    @"MyApp\.Functions\..*"
);

var app = builder.Build();
```

### Manual Component Registration

```csharp
public class MyService
{
    private readonly IAdvancedRegistry _registry;
    
    public MyService(IAdvancedRegistry registry)
    {
        _registry = registry;
    }
    
    public void RegisterComponents()
    {
        // Register LLM function with metadata
        var plannerFunction = new PlannerFunction();
        var metadata = new LLMFunctionMetadata
        {
            Name = "planner",
            Role = "planner",
            Description = "Analyzes user requests and creates execution plans",
            Category = "LLM",
            Tags = new List<string> { "planning", "analysis" },
            SupportedModels = new List<string> { "gpt-4", "gpt-3.5-turbo" },
            RequiredParameters = new List<string> { "user_request", "available_tools" }
        };
        
        var registrationId = _registry.RegisterLLMFunction(plannerFunction, metadata);
        
        // Register tool with metadata
        var webSearchTool = new WebSearchTool();
        var toolMetadata = new ToolMetadata
        {
            Name = "web_search",
            Description = "Searches the web for information",
            Category = "Search",
            ToolType = ToolType.BuiltIn,
            TimeoutSeconds = 30,
            IsCacheable = true,
            CacheTTLMinutes = 15
        };
        
        _registry.RegisterTool(webSearchTool, toolMetadata);
    }
}
```

### Component Retrieval

```csharp
public class OrchestrationService
{
    private readonly IAdvancedRegistry _registry;
    
    public OrchestrationService(IAdvancedRegistry registry)
    {
        _registry = registry;
    }
    
    public async Task ExecuteAsync(string userRequest)
    {
        // Get specific LLM function
        var planner = _registry.GetLLMFunction("planner");
        if (planner != null)
        {
            // Use the planner function
            var plan = await planner.ExecuteAsync(context);
        }
        
        // Get specific tool
        var webSearch = _registry.GetTool("web_search");
        if (webSearch != null)
        {
            // Use the web search tool
            var result = await webSearch.ExecuteAsync(input);
        }
        
        // Get all LLM functions
        var allFunctions = _registry.GetAllLLMFunctions();
        
        // Get all tools
        var allTools = _registry.GetAllTools();
    }
}
```

## Advanced Features

### Component Discovery

```csharp
public class ComponentManager
{
    private readonly IAdvancedRegistry _registry;
    
    public ComponentManager(IAdvancedRegistry registry)
    {
        _registry = registry;
    }
    
    public void DiscoverComponents()
    {
        // Find components by tags
        var searchComponents = _registry.FindComponentsByTags("search", "web");
        
        // Find components by category
        var llmComponents = _registry.FindComponentsByCategory("LLM");
        
        // Find components by type
        var tools = _registry.FindComponentsByType<ITool>();
        
        // Get all registrations with metadata
        var allRegistrations = _registry.GetAllRegistrations();
        foreach (var registration in allRegistrations)
        {
            Console.WriteLine($"Component: {registration.Metadata.Name}");
            Console.WriteLine($"Type: {registration.Metadata.GetType().Name}");
            Console.WriteLine($"Usage Count: {registration.UsageCount}");
            Console.WriteLine($"Last Used: {registration.LastUsedAt}");
        }
    }
}
```

### Component Management

```csharp
public class ComponentController
{
    private readonly IAdvancedRegistry _registry;
    
    public ComponentController(IAdvancedRegistry registry)
    {
        _registry = registry;
    }
    
    public void ManageComponents()
    {
        // Enable/disable components
        _registry.SetComponentEnabled("web_search", false);
        _registry.SetComponentEnabled("planner", true);
        
        // Get component metadata
        var metadata = _registry.GetComponentMetadata("planner");
        if (metadata is LLMFunctionMetadata llmMetadata)
        {
            Console.WriteLine($"Role: {llmMetadata.Role}");
            Console.WriteLine($"Supported Models: {string.Join(", ", llmMetadata.SupportedModels)}");
        }
        
        // Get registry status
        var status = _registry.GetRegistryStatus();
        Console.WriteLine($"Total Components: {status.TotalComponents}");
        Console.WriteLine($"LLM Functions: {status.LLMFunctionCount}");
        Console.WriteLine($"Tools: {status.ToolCount}");
        Console.WriteLine($"Enabled: {status.EnabledComponents}");
        Console.WriteLine($"Disabled: {status.DisabledComponents}");
        
        // Category statistics
        foreach (var category in status.CategoryStatistics)
        {
            Console.WriteLine($"{category.Key}: {category.Value}");
        }
    }
}
```

### Automatic Assembly Scanning

```csharp
public class PluginManager
{
    private readonly IAdvancedRegistry _registry;
    
    public PluginManager(IAdvancedRegistry registry)
    {
        _registry = registry;
    }
    
    public void LoadPlugins()
    {
        // Load from specific assembly
        var pluginAssembly = Assembly.LoadFrom("MyPlugins.dll");
        var registeredCount = _registry.AutoRegisterFromAssembly<ITool>(pluginAssembly);
        Console.WriteLine($"Registered {registeredCount} tools from plugin assembly");
        
        // Load from multiple assemblies
        var assemblies = new[]
        {
            Assembly.LoadFrom("Plugins.Search.dll"),
            Assembly.LoadFrom("Plugins.Database.dll"),
            Assembly.LoadFrom("Plugins.AI.dll")
        };
        _registry.AutoRegisterFromAssemblies(assemblies);
        
        // Load from namespace pattern
        var coreAssembly = Assembly.GetExecutingAssembly();
        _registry.AutoRegisterFromNamespace(coreAssembly, @"MyApp\.LLM\.Functions\..*");
    }
}
```

## Component Metadata

### LLM Function Metadata

```csharp
var llmMetadata = new LLMFunctionMetadata
{
    Name = "summarizer",
    Role = "summarizer",
    Description = "Summarizes long text content",
    Version = "1.2.0",
    Author = "AI Team",
    Category = "LLM",
    Tags = new List<string> { "text", "summarization", "nlp" },
    SupportedModels = new List<string> { "gpt-4", "claude-3-sonnet" },
    RequiredParameters = new List<string> { "text_content", "max_length" },
    OptionalParameters = new List<string> { "style", "language" },
    ResponseSchema = @"{
        ""type"": ""object"",
        ""properties"": {
            ""summary"": { ""type"": ""string"" },
            ""key_points"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } }
        }
    }",
    Priority = 1
};
```

### Tool Metadata

```csharp
var toolMetadata = new ToolMetadata
{
    Name = "database_query",
    Description = "Executes SQL queries against the database",
    Version = "2.1.0",
    Author = "Data Team",
    Category = "Database",
    Tags = new List<string> { "sql", "database", "query" },
    ToolType = ToolType.BuiltIn,
    InputSchema = @"{
        ""type"": ""object"",
        ""properties"": {
            ""query"": { ""type"": ""string"" },
            ""parameters"": { ""type"": ""object"" }
        },
        ""required"": [""query""]
    }",
    OutputSchema = @"{
        ""type"": ""object"",
        ""properties"": {
            ""rows"": { ""type"": ""array"" },
            ""count"": { ""type"": ""integer"" }
        }
    }",
    Dependencies = new List<string> { "System.Data.SqlClient" },
    RequiredPermissions = new List<string> { "database.read", "database.write" },
    TimeoutSeconds = 60,
    IsCacheable = true,
    CacheTTLMinutes = 30
};
```

## Component Discovery Utility

```csharp
public class DiscoveryService
{
    private readonly IComponentDiscovery _discovery;
    
    public DiscoveryService(IComponentDiscovery discovery)
    {
        _discovery = discovery;
    }
    
    public void DiscoverAndValidate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Discover LLM functions
        var llmTypes = _discovery.DiscoverLLMFunctions(assembly);
        foreach (var type in llmTypes)
        {
            if (_discovery.ValidateComponent(type))
            {
                var metadata = _discovery.ExtractMetadata(type);
                Console.WriteLine($"Valid LLM Function: {metadata.Name}");
            }
        }
        
        // Discover tools
        var toolTypes = _discovery.DiscoverTools(assembly);
        foreach (var type in toolTypes)
        {
            if (_discovery.ValidateComponent(type))
            {
                var metadata = _discovery.ExtractMetadata(type);
                Console.WriteLine($"Valid Tool: {metadata.Name}");
            }
        }
        
        // Discover by namespace
        var namespaceTypes = _discovery.DiscoverTypesByNamespace(assembly, @"MyApp\.Plugins\..*");
        Console.WriteLine($"Found {namespaceTypes.Count} components in plugin namespace");
        
        // Discover assemblies in directory
        var assemblies = _discovery.DiscoverAssemblies("./plugins", "*.dll");
        Console.WriteLine($"Found {assemblies.Count} plugin assemblies");
    }
}
```

## Error Handling

The Registry system provides specific exceptions for different error scenarios:

```csharp
try
{
    var component = _registry.GetLLMFunction("nonexistent");
}
catch (ComponentNotFoundException ex)
{
    Console.WriteLine($"Component not found: {ex.ComponentName}");
}
catch (ComponentRegistrationException ex)
{
    Console.WriteLine($"Registration failed: {ex.Message}");
    if (ex.ComponentType != null)
    {
        Console.WriteLine($"Component type: {ex.ComponentType.Name}");
    }
}
catch (DuplicateComponentException ex)
{
    Console.WriteLine($"Duplicate component: {ex.ComponentName}");
}
catch (AssemblyScanException ex)
{
    Console.WriteLine($"Assembly scan failed: {ex.AssemblyName} - {ex.Message}");
}
catch (RegistryException ex)
{
    Console.WriteLine($"Registry error: {ex.Message}");
}
```

## Best Practices

1. **Use Dependency Injection**: Register the registry as a singleton service
2. **Metadata Completeness**: Provide comprehensive metadata for better discoverability
3. **Component Validation**: Always validate components before registration
4. **Error Handling**: Handle registry exceptions appropriately
5. **Performance Monitoring**: Monitor component usage statistics
6. **Lazy Loading**: Use lazy loading for expensive component initialization
7. **Thread Safety**: The registry is thread-safe for concurrent access
8. **Resource Cleanup**: Properly dispose of components when unregistering

## Integration Examples

### With ASP.NET Core

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRegistry();
builder.Services.AddRegistryWithCurrentAssembly();

var app = builder.Build();

// Initialize registry on startup
var registry = app.Services.GetRequiredService<IRegistryInitializer>();
await registry.InitializeAsync();

app.Run();
```

### With Background Services

```csharp
public class ComponentMonitoringService : BackgroundService
{
    private readonly IAdvancedRegistry _registry;
    private readonly ILogger<ComponentMonitoringService> _logger;
    
    public ComponentMonitoringService(
        IAdvancedRegistry registry,
        ILogger<ComponentMonitoringService> logger)
    {
        _registry = registry;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var status = _registry.GetRegistryStatus();
            _logger.LogInformation("Registry Status - Total: {Total}, Enabled: {Enabled}", 
                status.TotalComponents, status.EnabledComponents);
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## Troubleshooting

### Common Issues

1. **Components Not Found**: Check assembly loading and namespace patterns
2. **Registration Failures**: Verify component constructors and dependencies
3. **Metadata Missing**: Ensure proper metadata configuration
4. **Performance Issues**: Monitor component usage and optimize hot paths

### Debug Information

```csharp
// Enable detailed logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Get detailed registry information
var status = _registry.GetRegistryStatus();
Console.WriteLine($"Registry Status: {JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true })}");
```

The Registry system provides a robust foundation for component management in the AI Agent Framework, enabling flexible and scalable architecture for LLM functions and tools.