namespace AIAgentFramework.Core.Validation;

/// <summary>
/// 검증자 인터페이스
/// </summary>
/// <typeparam name="T">검증할 객체 타입</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// 객체를 검증합니다.
    /// </summary>
    /// <param name="item">검증할 객체</param>
    /// <returns>검증 결과</returns>
    IValidationResult Validate(T item);
    
    /// <summary>
    /// 객체를 비동기로 검증합니다.
    /// </summary>
    /// <param name="item">검증할 객체</param>
    /// <returns>검증 결과</returns>
    Task<IValidationResult> ValidateAsync(T item);
}