using System.Collections.Generic;
using AIAgentFramework.Core.Actions.Abstractions;

namespace AIAgentFramework.Core.Actions.Factories
{
    /// <summary>
    /// 타입 안전한 액션 생성을 위한 팩토리 인터페이스
    /// </summary>
    public interface IActionFactory
    {
        /// <summary>
        /// LLM 액션 생성
        /// </summary>
        /// <param name="functionName">LLM 기능 이름</param>
        /// <param name="parameters">매개변수</param>
        /// <returns>LLM 액션</returns>
        IOrchestrationAction CreateLLMAction(string functionName, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 도구 액션 생성
        /// </summary>
        /// <param name="toolName">도구 이름</param>
        /// <param name="parameters">매개변수</param>
        /// <returns>도구 액션</returns>
        IOrchestrationAction CreateToolAction(string toolName, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// JSON 객체에서 액션 생성
        /// </summary>
        /// <param name="actionData">액션 데이터 (JSON 파싱 결과)</param>
        /// <returns>생성된 액션</returns>
        IOrchestrationAction CreateFromJsonObject(Dictionary<string, object> actionData);

        /// <summary>
        /// 여러 액션을 일괄 생성
        /// </summary>
        /// <param name="actionsData">액션 데이터 배열</param>
        /// <returns>생성된 액션 목록</returns>
        List<IOrchestrationAction> CreateActionsFromJsonArray(List<Dictionary<string, object>> actionsData);
    }
}