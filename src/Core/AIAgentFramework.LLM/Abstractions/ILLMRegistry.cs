namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 기능 Registry 인터페이스
/// 모든 LLM 기능을 관리하는 중앙 저장소
/// </summary>
public interface ILLMRegistry
{
    /// <summary>
    /// LLM 기능 등록
    /// </summary>
    /// <param name="function">등록할 LLM 기능</param>
    void Register(ILLMFunction function);

    /// <summary>
    /// 역할로 LLM 기능 조회
    /// </summary>
    /// <param name="role">LLM 역할</param>
    /// <returns>LLM 기능 인스턴스</returns>
    ILLMFunction? GetFunction(LLMRole role);

    /// <summary>
    /// 모든 LLM 기능 목록 조회
    /// </summary>
    /// <returns>등록된 모든 LLM 기능</returns>
    IReadOnlyCollection<ILLMFunction> GetAllFunctions();

    /// <summary>
    /// LLM 제공자 등록
    /// </summary>
    /// <param name="provider">LLM 제공자</param>
    void RegisterProvider(ILLMProvider provider);

    /// <summary>
    /// LLM 제공자 조회
    /// </summary>
    /// <param name="providerName">제공자 이름</param>
    /// <returns>LLM 제공자 인스턴스</returns>
    ILLMProvider? GetProvider(string providerName);

    /// <summary>
    /// LLM에게 제공할 Function 설명 목록 생성
    /// </summary>
    /// <returns>LLM이 이해할 수 있는 형식의 Function 설명 (JSON 등)</returns>
    string GetFunctionDescriptionsForLLM();
}
