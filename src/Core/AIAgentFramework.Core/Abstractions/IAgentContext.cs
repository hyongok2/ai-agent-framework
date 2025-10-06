namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// Agent 전체 컨텍스트 - 실행 메타정보 + 동적 변수 저장소
/// - IExecutionContext: 실행 메타정보 (ExecutionId, UserId, SessionId 등)
/// - 동적 변수: Plan 실행 중 Step 간 데이터 공유 (fileContent, summary 등)
/// - LLM 호출 시 변수 치환에 사용
/// </summary>
public interface IAgentContext : IExecutionContext
{
    /// <summary>
    /// 컨텍스트 변수 저장소 (Step 간 데이터 공유용)
    /// </summary>
    Dictionary<string, object> Variables { get; }

    /// <summary>
    /// 값 설정
    /// </summary>
    void Set(string key, object value);

    /// <summary>
    /// 값 가져오기
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// 값 가져오기 (안전)
    /// </summary>
    bool TryGet<T>(string key, out T? value);

    /// <summary>
    /// 키 존재 여부
    /// </summary>
    bool Contains(string key);

    /// <summary>
    /// 모든 키 목록
    /// </summary>
    IEnumerable<string> Keys { get; }
}
