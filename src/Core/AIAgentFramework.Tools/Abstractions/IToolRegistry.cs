namespace AIAgentFramework.Tools.Abstractions;

/// <summary>
/// Tool Registry 인터페이스
/// 모든 Tool을 동등하게 관리하는 중앙 저장소
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Tool 등록
    /// </summary>
    /// <param name="tool">등록할 Tool</param>
    void Register(ITool tool);

    /// <summary>
    /// 이름으로 Tool 조회
    /// </summary>
    /// <param name="name">Tool 이름</param>
    /// <returns>Tool 인스턴스</returns>
    ITool? GetTool(string name);

    /// <summary>
    /// 모든 Tool 목록 조회
    /// </summary>
    /// <returns>등록된 모든 Tool</returns>
    IReadOnlyCollection<ITool> GetAllTools();

    /// <summary>
    /// 유형별 Tool 목록 조회
    /// </summary>
    /// <param name="type">Tool 유형</param>
    /// <returns>해당 유형의 모든 Tool</returns>
    IReadOnlyCollection<ITool> GetToolsByType(ToolType type);

    /// <summary>
    /// LLM에 제공할 Tool 설명 목록 생성
    /// </summary>
    /// <returns>Tool 설명 목록 (JSON 형식)</returns>
    string GetToolDescriptionsForLLM();
}
