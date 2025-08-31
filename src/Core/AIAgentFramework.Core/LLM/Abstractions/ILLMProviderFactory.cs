namespace AIAgentFramework.Core.LLM.Abstractions;

/// <summary>
/// LLM Provider Factory 인터페이스
/// </summary>
public interface ILLMProviderFactory
{
    /// <summary>
    /// Provider 타입으로 Provider 생성
    /// </summary>
    /// <param name="providerType">Provider 타입 (openai, claude, local 등)</param>
    /// <returns>LLM Provider</returns>
    ILLMProvider CreateProvider(string providerType);
    
    /// <summary>
    /// 역할에 따른 Provider 생성
    /// </summary>
    /// <param name="role">역할 (planner, interpreter, summarizer 등)</param>
    /// <returns>LLM Provider</returns>
    ILLMProvider CreateProviderForRole(string role);
    
    /// <summary>
    /// 지원되는 Provider 타입 목록
    /// </summary>
    /// <returns>Provider 타입 목록</returns>
    List<string> GetSupportedProviderTypes();
    
    /// <summary>
    /// 기본 Provider 생성
    /// </summary>
    /// <returns>기본 LLM Provider</returns>
    ILLMProvider CreateDefaultProvider();
}