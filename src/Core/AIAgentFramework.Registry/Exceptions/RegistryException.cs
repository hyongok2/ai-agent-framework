using AIAgentFramework.Core.Exceptions;

namespace AIAgentFramework.Registry.Exceptions;

/// <summary>
/// 레지스트리 관련 예외
/// </summary>
public class RegistryException : AIAgentFrameworkException
{
    /// <summary>
    /// 생성자
    /// </summary>
    public RegistryException() : base("Registry operation failed")
    {
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    public RegistryException(string message) : base(message)
    {
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    /// <param name="innerException">내부 예외</param>
    public RegistryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// 컴포넌트를 찾을 수 없는 예외
/// </summary>
public class ComponentNotFoundException : RegistryException
{
    /// <summary>
    /// 컴포넌트 이름
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="componentName">컴포넌트 이름</param>
    public ComponentNotFoundException(string componentName) 
        : base($"Component not found: {componentName}")
    {
        ComponentName = componentName;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="componentName">컴포넌트 이름</param>
    /// <param name="innerException">내부 예외</param>
    public ComponentNotFoundException(string componentName, Exception innerException) 
        : base($"Component not found: {componentName}", innerException)
    {
        ComponentName = componentName;
    }
}

/// <summary>
/// 컴포넌트 등록 실패 예외
/// </summary>
public class ComponentRegistrationException : RegistryException
{
    /// <summary>
    /// 컴포넌트 타입
    /// </summary>
    public Type? ComponentType { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    public ComponentRegistrationException(string message) : base(message)
    {
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    /// <param name="componentType">컴포넌트 타입</param>
    public ComponentRegistrationException(string message, Type componentType) : base(message)
    {
        ComponentType = componentType;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    /// <param name="componentType">컴포넌트 타입</param>
    /// <param name="innerException">내부 예외</param>
    public ComponentRegistrationException(string message, Type componentType, Exception innerException) 
        : base(message, innerException)
    {
        ComponentType = componentType;
    }
}

/// <summary>
/// 중복 컴포넌트 등록 예외
/// </summary>
public class DuplicateComponentException : ComponentRegistrationException
{
    /// <summary>
    /// 컴포넌트 이름
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="componentName">컴포넌트 이름</param>
    public DuplicateComponentException(string componentName) 
        : base($"Component already registered: {componentName}")
    {
        ComponentName = componentName;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="componentName">컴포넌트 이름</param>
    /// <param name="componentType">컴포넌트 타입</param>
    public DuplicateComponentException(string componentName, Type componentType) 
        : base($"Component already registered: {componentName}", componentType)
    {
        ComponentName = componentName;
    }
}

/// <summary>
/// 어셈블리 스캔 실패 예외
/// </summary>
public class AssemblyScanException : RegistryException
{
    /// <summary>
    /// 어셈블리 이름
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="assemblyName">어셈블리 이름</param>
    /// <param name="message">예외 메시지</param>
    public AssemblyScanException(string assemblyName, string message) 
        : base($"Assembly scan failed for '{assemblyName}': {message}")
    {
        AssemblyName = assemblyName;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="assemblyName">어셈블리 이름</param>
    /// <param name="message">예외 메시지</param>
    /// <param name="innerException">내부 예외</param>
    public AssemblyScanException(string assemblyName, string message, Exception innerException) 
        : base($"Assembly scan failed for '{assemblyName}': {message}", innerException)
    {
        AssemblyName = assemblyName;
    }
}