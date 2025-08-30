namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// LLM 응답 파서 인터페이스
/// </summary>
public interface ILLMResponseParser
{
    /// <summary>
    /// 응답 파싱
    /// </summary>
    /// <typeparam name="T">파싱할 타입</typeparam>
    /// <param name="response">LLM 응답</param>
    /// <returns>파싱된 결과</returns>
    T? Parse<T>(string response) where T : class;
    
    /// <summary>
    /// JSON 응답 파싱
    /// </summary>
    /// <typeparam name="T">파싱할 타입</typeparam>
    /// <param name="response">LLM 응답</param>
    /// <returns>파싱된 결과</returns>
    T? ParseJson<T>(string response) where T : class;
    
    /// <summary>
    /// 구조화된 응답 검증
    /// </summary>
    /// <param name="response">LLM 응답</param>
    /// <param name="expectedFields">필수 필드 목록</param>
    /// <returns>검증 결과</returns>
    bool ValidateStructuredResponse(string response, params string[] expectedFields);
    
    /// <summary>
    /// JSON 블록 추출
    /// </summary>
    /// <param name="response">LLM 응답</param>
    /// <returns>JSON 문자열</returns>
    string ExtractJsonBlock(string response);
    
    /// <summary>
    /// 특수 응답 타입 감지
    /// </summary>
    /// <param name="response">LLM 응답</param>
    /// <returns>응답 타입</returns>
    LLMResponseType DetectResponseType(string response);
}

/// <summary>
/// LLM 응답 타입
/// </summary>
public enum LLMResponseType
{
    /// <summary>
    /// 일반 텍스트
    /// </summary>
    PlainText,
    
    /// <summary>
    /// JSON 구조화된 응답
    /// </summary>
    StructuredJson,
    
    /// <summary>
    /// 사용자 질문 응답
    /// </summary>
    UserQuestion,
    
    /// <summary>
    /// 오류 응답
    /// </summary>
    Error,
    
    /// <summary>
    /// 완료 응답
    /// </summary>
    Completion,
    
    /// <summary>
    /// 특수 기능 호출
    /// </summary>
    SpecialFunction
}