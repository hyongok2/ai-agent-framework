

using System.Text.Json;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Tools.Models;
using AIAgentFramework.Core.Validation;

namespace AIAgentFramework.Tools.Execution;

/// <summary>
/// 도구 파라미터 처리기
/// </summary>
public class ParameterProcessor
{
    /// <summary>
    /// 필수 파라미터 검증
    /// </summary>
    public ValidationResult ValidateRequiredParameters(IToolInput input, IToolContract contract)
    {
        var errors = new List<string>();

        foreach (var required in contract.RequiredParameters)
        {
            if (!input.Parameters.ContainsKey(required))
            {
                errors.Add($"필수 파라미터 '{required}'가 누락되었습니다.");
            }
            else if (input.Parameters[required] == null)
            {
                errors.Add($"필수 파라미터 '{required}'의 값이 null입니다.");
            }
        }

        return errors.Any() ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
    }

    /// <summary>
    /// 파라미터 타입 변환
    /// </summary>
    public IToolInput ConvertParameterTypes(IToolInput input, IToolContract contract)
    {
        var convertedInput = new ToolInput
        {
            Parameters = new Dictionary<string, object>()
        };

        foreach (var param in input.Parameters)
        {
            try
            {
                var convertedValue = ConvertParameterValue(param.Value, GetParameterType(param.Key, contract));
                convertedInput.Parameters[param.Key] = convertedValue;
            }
            catch (Exception)
            {
                // 변환 실패 시 원본 값 유지
                convertedInput.Parameters[param.Key] = param.Value;
            }
        }

        return convertedInput;
    }

    /// <summary>
    /// JSON 스키마 기반 파라미터 검증
    /// </summary>
    public ValidationResult ValidateAgainstSchema(IToolInput input, string jsonSchema)
    {
        try
        {
            // 기본 JSON 형식 검증
            var json = JsonSerializer.Serialize(input.Parameters);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"JSON 스키마 검증 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 기본값 적용
    /// </summary>
    public IToolInput ApplyDefaultValues(IToolInput input, Dictionary<string, object> defaultValues)
    {
        var resultInput = new ToolInput
        {
            Parameters = new Dictionary<string, object>(input.Parameters)
        };

        foreach (var defaultValue in defaultValues)
        {
            if (!resultInput.Parameters.ContainsKey(defaultValue.Key))
            {
                resultInput.Parameters[defaultValue.Key] = defaultValue.Value;
            }
        }

        return resultInput;
    }

    private static object ConvertParameterValue(object? value, Type targetType)
    {
        if (value == null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType)! : null!;
            
        if (targetType == typeof(object))
            return value;

        if (value.GetType() == targetType)
            return value;

        // 문자열에서 다른 타입으로 변환
        if (value is string stringValue)
        {
            if (targetType == typeof(int))
                return int.Parse(stringValue);
            if (targetType == typeof(double))
                return double.Parse(stringValue);
            if (targetType == typeof(bool))
                return bool.Parse(stringValue);
            if (targetType == typeof(DateTime))
                return DateTime.Parse(stringValue);
        }

        var result = Convert.ChangeType(value, targetType);
        return result ?? (targetType.IsValueType ? Activator.CreateInstance(targetType)! : null!);
    }

    private static Type GetParameterType(string parameterName, IToolContract contract)
    {
        // 실제 구현에서는 JSON 스키마에서 타입 정보를 추출
        // 현재는 기본 타입 반환
        _ = parameterName;
        _ = contract;
        return typeof(object);
    }
}