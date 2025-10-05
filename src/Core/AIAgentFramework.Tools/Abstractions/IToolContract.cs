namespace AIAgentFramework.Tools.Abstractions;

/// <summary>
/// Tool 계약 인터페이스
/// Tool의 입출력 스키마 정의
/// </summary>
public interface IToolContract
{
    /// <summary>
    /// 입력 스키마 (JSON Schema 형식)
    /// </summary>
    string InputSchema { get; }

    /// <summary>
    /// 출력 스키마 (JSON Schema 형식)
    /// </summary>
    string OutputSchema { get; }

    /// <summary>
    /// 파라미터가 필요한지 여부
    /// </summary>
    bool RequiresParameters { get; }

    /// <summary>
    /// 입력 데이터 검증
    /// </summary>
    /// <param name="input">입력 데이터</param>
    /// <returns>검증 결과</returns>
    bool ValidateInput(object? input);
}
