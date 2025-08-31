
using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Registry.Models;
using System.Reflection;

namespace AIAgentFramework.Registry;

/// <summary>
/// 고급 레지스트리 인터페이스
/// </summary>
public interface IAdvancedRegistry : IRegistry
{
    /// <summary>
    /// 메타데이터와 함께 LLM 기능을 등록합니다.
    /// </summary>
    /// <param name="function">LLM 기능</param>
    /// <param name="metadata">메타데이터</param>
    /// <returns>등록 ID</returns>
    string RegisterLLMFunction(ILLMFunction function, LLMFunctionMetadata metadata);
    
    /// <summary>
    /// 메타데이터와 함께 도구를 등록합니다.
    /// </summary>
    /// <param name="tool">도구</param>
    /// <param name="metadata">메타데이터</param>
    /// <returns>등록 ID</returns>
    string RegisterTool(ITool tool, ToolMetadata metadata);
    
    /// <summary>
    /// 컴포넌트를 등록 해제합니다.
    /// </summary>
    /// <param name="registrationId">등록 ID</param>
    /// <returns>등록 해제 성공 여부</returns>
    bool UnregisterComponent(string registrationId);
    
    /// <summary>
    /// 컴포넌트 메타데이터를 조회합니다.
    /// </summary>
    /// <param name="name">컴포넌트 이름</param>
    /// <returns>메타데이터</returns>
    ComponentMetadata? GetComponentMetadata(string name);
    
    /// <summary>
    /// 태그로 컴포넌트를 검색합니다.
    /// </summary>
    /// <param name="tags">태그 목록</param>
    /// <returns>컴포넌트 등록 정보 목록</returns>
    List<ComponentRegistration> FindComponentsByTags(params string[] tags);
    
    /// <summary>
    /// 카테고리로 컴포넌트를 검색합니다.
    /// </summary>
    /// <param name="category">카테고리</param>
    /// <returns>컴포넌트 등록 정보 목록</returns>
    List<ComponentRegistration> FindComponentsByCategory(string category);
    
    /// <summary>
    /// 타입으로 컴포넌트를 검색합니다.
    /// </summary>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    /// <returns>컴포넌트 목록</returns>
    List<T> FindComponentsByType<T>() where T : class;
    
    /// <summary>
    /// 모든 등록된 컴포넌트를 조회합니다.
    /// </summary>
    /// <returns>컴포넌트 등록 정보 목록</returns>
    List<ComponentRegistration> GetAllRegistrations();
    
    /// <summary>
    /// 컴포넌트 사용 통계를 업데이트합니다.
    /// </summary>
    /// <param name="name">컴포넌트 이름</param>
    void UpdateUsageStatistics(string name);
    
    /// <summary>
    /// 컴포넌트 활성화/비활성화를 설정합니다.
    /// </summary>
    /// <param name="name">컴포넌트 이름</param>
    /// <param name="enabled">활성화 여부</param>
    void SetComponentEnabled(string name, bool enabled);
    
    /// <summary>
    /// 레지스트리 상태를 조회합니다.
    /// </summary>
    /// <returns>레지스트리 상태</returns>
    RegistryStatus GetRegistryStatus();
    
    /// <summary>
    /// 레지스트리를 초기화합니다.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// 어셈블리에서 특정 타입의 컴포넌트만 자동 등록합니다.
    /// </summary>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    /// <param name="assembly">어셈블리</param>
    /// <returns>등록된 컴포넌트 수</returns>
    int AutoRegisterFromAssembly<T>(Assembly assembly) where T : class;
    
    /// <summary>
    /// 여러 어셈블리에서 자동 등록합니다.
    /// </summary>
    /// <param name="assemblies">어셈블리 목록</param>
    /// <returns>등록된 컴포넌트 수</returns>
    int AutoRegisterFromAssemblies(params Assembly[] assemblies);
    
    /// <summary>
    /// 네임스페이스 패턴으로 어셈블리에서 자동 등록합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <param name="namespacePattern">네임스페이스 패턴</param>
    /// <returns>등록된 컴포넌트 수</returns>
    int AutoRegisterFromNamespace(Assembly assembly, string namespacePattern);
    
    /// <summary>
    /// Attribute가 있는 컴포넌트만 자동 등록합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <returns>등록된 컴포넌트 수</returns>
    int AutoRegisterWithAttributes(Assembly assembly);
    
    /// <summary>
    /// 여러 어셈블리에서 Attribute 기반 자동 등록합니다.
    /// </summary>
    /// <param name="assemblies">어셈블리 목록</param>
    /// <returns>등록된 컴포넌트 수</returns>
    int AutoRegisterWithAttributesFromAssemblies(params Assembly[] assemblies);
    
    /// <summary>
    /// 특정 타입의 Attribute가 있는 컴포넌트만 등록합니다.
    /// </summary>
    /// <typeparam name="TAttribute">Attribute 타입</typeparam>
    /// <param name="assembly">어셈블리</param>
    /// <returns>등록된 컴포넌트 수</returns>
    int AutoRegisterByAttribute<TAttribute>(Assembly assembly) where TAttribute : Attribute;
}

/// <summary>
/// 레지스트리 상태
/// </summary>
public class RegistryStatus
{
    /// <summary>
    /// 총 등록된 컴포넌트 수
    /// </summary>
    public int TotalComponents { get; set; }
    
    /// <summary>
    /// LLM 기능 수
    /// </summary>
    public int LLMFunctionCount { get; set; }
    
    /// <summary>
    /// 도구 수
    /// </summary>
    public int ToolCount { get; set; }
    
    /// <summary>
    /// 활성화된 컴포넌트 수
    /// </summary>
    public int EnabledComponents { get; set; }
    
    /// <summary>
    /// 비활성화된 컴포넌트 수
    /// </summary>
    public int DisabledComponents { get; set; }
    
    /// <summary>
    /// 카테고리별 통계
    /// </summary>
    public Dictionary<string, int> CategoryStatistics { get; set; } = new();
    
    /// <summary>
    /// 태그별 통계
    /// </summary>
    public Dictionary<string, int> TagStatistics { get; set; } = new();
    
    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}