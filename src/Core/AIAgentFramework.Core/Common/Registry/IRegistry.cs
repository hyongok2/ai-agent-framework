using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.Tools.Abstractions;

namespace AIAgentFramework.Core.Common.Registry;

/// <summary>
/// 레지스트리 인터페이스
/// </summary>
public interface IRegistry
{
    /// <summary>
    /// LLM 기능을 등록합니다.
    /// </summary>
    /// <param name="function">LLM 기능</param>
    void RegisterLLMFunction(ILLMFunction function);
    
    /// <summary>
    /// 도구를 등록합니다.
    /// </summary>
    /// <param name="tool">도구</param>
    void RegisterTool(ITool tool);
    
    /// <summary>
    /// LLM 기능을 조회합니다.
    /// </summary>
    /// <param name="role">역할 이름</param>
    /// <returns>LLM 기능</returns>
    ILLMFunction? GetLLMFunction(string role);
    
    /// <summary>
    /// 도구를 조회합니다.
    /// </summary>
    /// <param name="name">도구 이름</param>
    /// <returns>도구</returns>
    ITool? GetTool(string name);
    
    /// <summary>
    /// 모든 LLM 기능을 조회합니다.
    /// </summary>
    /// <returns>LLM 기능 목록</returns>
    List<ILLMFunction> GetAllLLMFunctions();
    
    /// <summary>
    /// 모든 도구를 조회합니다.
    /// </summary>
    /// <returns>도구 목록</returns>
    List<ITool> GetAllTools();
    
    /// <summary>
    /// 어셈블리에서 자동으로 등록합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    void AutoRegisterFromAssembly(System.Reflection.Assembly assembly);
}