
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Validation;

namespace AIAgentFramework.Core.Tools.Models;

/// <summary>
/// 도구 계약 구현
/// </summary>
public class ToolContract : IToolContract
{
    /// <inheritdoc />
    public string InputSchema { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string OutputSchema { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public List<string> RequiredParameters { get; set; } = new();
    
    /// <inheritdoc />
    public List<string> OptionalParameters { get; set; } = new();

    /// <summary>
    /// 생성자
    /// </summary>
    public ToolContract() { }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="inputSchema">입력 스키마</param>
    /// <param name="outputSchema">출력 스키마</param>
    /// <param name="requiredParameters">필수 파라미터</param>
    /// <param name="optionalParameters">선택적 파라미터</param>
    public ToolContract(string inputSchema, string outputSchema, List<string>? requiredParameters = null, List<string>? optionalParameters = null)
    {
        InputSchema = inputSchema;
        OutputSchema = outputSchema;
        RequiredParameters = requiredParameters ?? new List<string>();
        OptionalParameters = optionalParameters ?? new List<string>();
    }

    /// <summary>
    /// 필수 파라미터 추가
    /// </summary>
    /// <param name="parameter">파라미터명</param>
    /// <returns>현재 계약</returns>
    public ToolContract WithRequiredParameter(string parameter)
    {
        if (!RequiredParameters.Contains(parameter))
        {
            RequiredParameters.Add(parameter);
        }
        return this;
    }

    /// <summary>
    /// 선택적 파라미터 추가
    /// </summary>
    /// <param name="parameter">파라미터명</param>
    /// <returns>현재 계약</returns>
    public ToolContract WithOptionalParameter(string parameter)
    {
        if (!OptionalParameters.Contains(parameter))
        {
            OptionalParameters.Add(parameter);
        }
        return this;
    }

    /// <summary>
    /// 입력 스키마 설정
    /// </summary>
    /// <param name="schema">JSON 스키마</param>
    /// <returns>현재 계약</returns>
    public ToolContract WithInputSchema(string schema)
    {
        InputSchema = schema;
        return this;
    }

    /// <summary>
    /// 출력 스키마 설정
    /// </summary>
    /// <param name="schema">JSON 스키마</param>
    /// <returns>현재 계약</returns>
    public ToolContract WithOutputSchema(string schema)
    {
        OutputSchema = schema;
        return this;
    }

    /// <inheritdoc />
    public ValidationResult ValidateInput(IToolInput input)
    {
        var errors = new List<string>();
        
        // 필수 파라미터 검증
        foreach (var required in RequiredParameters)
        {
            if (!input.Parameters.ContainsKey(required))
            {
                errors.Add($"필수 파라미터 '{required}'가 누락되었습니다.");
            }
        }
        
        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }

    /// <inheritdoc />
    public ValidationResult ValidateOutput(IToolResult output)
    {
        // 기본 출력 검증 (실제로는 JSON Schema 검증 구현 필요)
        return ValidationResult.Success();
    }
}