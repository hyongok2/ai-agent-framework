using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Tools.Execution;

/// <summary>
/// 도구 실행 결과 표준화 처리기
/// </summary>
public class ToolResultStandardizer
{
    /// <summary>
    /// 도구 결과를 표준 형식으로 변환
    /// </summary>
    public IToolResult StandardizeResult(IToolResult result, string toolName, string toolType)
    {
        if (result == null)
            return ToolResult.CreateFailure("결과가 null입니다.");

        var standardizedResult = new ToolResult
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            ExecutionTime = result.ExecutionTime,
            Data = new Dictionary<string, object>(result.Data),
            Metadata = new Dictionary<string, object>(result.Metadata)
        };

        // 표준 메타데이터 추가
        standardizedResult.Metadata["tool_name"] = toolName;
        standardizedResult.Metadata["tool_type"] = toolType;
        standardizedResult.Metadata["execution_timestamp"] = DateTime.UtcNow;
        standardizedResult.Metadata["framework_version"] = "1.0.0";

        // 성공 여부에 따른 추가 처리
        if (result.Success)
        {
            standardizedResult.Metadata["status"] = "completed";
            EnsureDataStructure(standardizedResult);
        }
        else
        {
            standardizedResult.Metadata["status"] = "failed";
            standardizedResult.Metadata["error_category"] = CategorizeError(result.ErrorMessage);
        }

        return standardizedResult;
    }

    /// <summary>
    /// 여러 도구 결과를 병합
    /// </summary>
    public IToolResult MergeResults(IEnumerable<IToolResult> results, string operationName)
    {
        var resultList = results.ToList();
        if (!resultList.Any())
            return ToolResult.CreateFailure("병합할 결과가 없습니다.");

        var mergedResult = new ToolResult
        {
            Success = resultList.All(r => r.Success),
            ExecutionTime = TimeSpan.FromMilliseconds(resultList.Sum(r => r.ExecutionTime.TotalMilliseconds)),
            Data = new Dictionary<string, object>(),
            Metadata = new Dictionary<string, object>()
        };

        // 데이터 병합
        mergedResult.Data["operation"] = operationName;
        mergedResult.Data["results"] = resultList.Select(r => new
        {
            success = r.Success,
            data = r.Data,
            error = r.ErrorMessage
        }).ToList();

        // 메타데이터 병합
        mergedResult.Metadata["total_operations"] = resultList.Count;
        mergedResult.Metadata["successful_operations"] = resultList.Count(r => r.Success);
        mergedResult.Metadata["failed_operations"] = resultList.Count(r => !r.Success);
        mergedResult.Metadata["execution_timestamp"] = DateTime.UtcNow;

        // 오류 메시지 설정
        if (!mergedResult.Success)
        {
            var errors = resultList.Where(r => !r.Success).Select(r => r.ErrorMessage).ToList();
            mergedResult.ErrorMessage = string.Join("; ", errors);
        }

        return mergedResult;
    }

    private void EnsureDataStructure(ToolResult result)
    {
        // 기본 데이터 구조 보장
        if (!result.Data.ContainsKey("result_type"))
            result.Data["result_type"] = "tool_execution";

        if (!result.Data.ContainsKey("content"))
            result.Data["content"] = result.Data.FirstOrDefault().Value ?? "No content";
    }

    private string CategorizeError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return "unknown";

        var message = errorMessage.ToLowerInvariant();
        
        if (message.Contains("timeout") || message.Contains("시간초과"))
            return "timeout";
        
        if (message.Contains("network") || message.Contains("connection") || message.Contains("네트워크"))
            return "network";
        
        if (message.Contains("permission") || message.Contains("권한") || message.Contains("unauthorized"))
            return "permission";
        
        if (message.Contains("validation") || message.Contains("검증") || message.Contains("invalid"))
            return "validation";
        
        if (message.Contains("not found") || message.Contains("찾을 수 없"))
            return "not_found";

        return "execution";
    }
}