

using System.Text.Json;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Common.Validation;

namespace AIAgentFramework.ErrorHandling.Validation;

public class ContractValidator
{
    public ValidationResult ValidateToolInput(IToolInput input, IToolContract contract)
    {
        var errors = new List<string>();

        foreach (var required in contract.RequiredParameters)
        {
            if (!input.Parameters.ContainsKey(required))
            {
                errors.Add($"필수 파라미터 '{required}'가 누락되었습니다.");
            }
        }

        if (!string.IsNullOrEmpty(contract.InputSchema))
        {
            try
            {
                var json = JsonSerializer.Serialize(input.Parameters);
                JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }
            catch (JsonException ex)
            {
                errors.Add($"JSON 형식 오류: {ex.Message}");
            }
        }

        return errors.Any() ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
    }

    public ValidationResult ValidateToolOutput(IToolResult result, IToolContract contract)
    {
        var errors = new List<string>();

        if (!result.Success && string.IsNullOrEmpty(result.ErrorMessage))
        {
            errors.Add("실패한 결과에는 오류 메시지가 필요합니다.");
        }

        if (result.Success && result.Data == null)
        {
            errors.Add("성공한 결과에는 데이터가 필요합니다.");
        }

        return errors.Any() ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
    }
}