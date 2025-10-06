namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// Agent 전체에서 공유되는 글로벌 컨텍스트
/// - Plan 실행 중 모든 Step 결과 저장
/// - LLM 호출 시 치환 변수로 사용
/// </summary>
public interface IAgentContext
{
    /// <summary>
    /// 컨텍스트 변수 저장소
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
