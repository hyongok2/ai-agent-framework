using System.Reflection;

namespace AIAgentFramework.Registry.AttributeBasedRegistration;

/// <summary>
/// Attribute 기반 컴포넌트 등록기 인터페이스
/// </summary>
public interface IAttributeBasedComponentRegistrar
{
    /// <summary>
    /// 어셈블리에서 Attribute가 있는 모든 컴포넌트를 등록합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <returns>등록된 컴포넌트 수</returns>
    int RegisterFromAssembly(Assembly assembly);
    
    /// <summary>
    /// 어셈블리에서 LLM 기능을 등록합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <returns>등록된 LLM 기능 수</returns>
    int RegisterLLMFunctionsFromAssembly(Assembly assembly);
    
    /// <summary>
    /// 어셈블리에서 Built-In 도구를 등록합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <returns>등록된 Built-In 도구 수</returns>
    int RegisterBuiltInToolsFromAssembly(Assembly assembly);
    
    /// <summary>
    /// 어셈블리에서 플러그인 도구를 등록합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <returns>등록된 플러그인 도구 수</returns>
    int RegisterPluginToolsFromAssembly(Assembly assembly);
    
    /// <summary>
    /// 어셈블리에서 MCP 도구를 등록합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <returns>등록된 MCP 도구 수</returns>
    int RegisterMCPToolsFromAssembly(Assembly assembly);
    
    /// <summary>
    /// 타입의 등록 정보를 가져옵니다.
    /// </summary>
    /// <param name="type">타입</param>
    /// <returns>등록 정보</returns>
    ComponentRegistrationInfo GetRegistrationInfo(Type type);
}

/// <summary>
/// 컴포넌트 등록 정보
/// </summary>
public class ComponentRegistrationInfo
{
    /// <summary>
    /// 컴포넌트 타입
    /// </summary>
    public Type Type { get; set; } = typeof(object);
    
    /// <summary>
    /// 컴포넌트 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 등록 가능 여부
    /// </summary>
    public bool IsRegistrable { get; set; }
    
    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 컴포넌트 타입
    /// </summary>
    public ComponentType ComponentType { get; set; }
    
    /// <summary>
    /// Attribute 객체
    /// </summary>
    public Attribute? Attribute { get; set; }
    
    /// <summary>
    /// 등록 실패 이유
    /// </summary>
    public string? FailureReason { get; set; }
}

/// <summary>
/// 컴포넌트 타입 열거형
/// </summary>
public enum ComponentType
{
    /// <summary>
    /// 알 수 없음
    /// </summary>
    Unknown,
    
    /// <summary>
    /// LLM 기능
    /// </summary>
    LLMFunction,
    
    /// <summary>
    /// Built-In 도구
    /// </summary>
    BuiltInTool,
    
    /// <summary>
    /// 플러그인 도구
    /// </summary>
    PluginTool,
    
    /// <summary>
    /// MCP 도구
    /// </summary>
    MCPTool
}