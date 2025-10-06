using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Tools.BuiltIn.DirectoryReader;

/// <summary>
/// 디렉토리 읽기 도구
/// 지정된 디렉토리의 파일 및 하위 디렉토리 목록을 반환
/// </summary>
public class DirectoryReaderTool : ITool
{
    public IToolMetadata Metadata { get; }
    public IToolContract Contract { get; }

    public DirectoryReaderTool()
    {
        Metadata = new ToolMetadata(
            name: "DirectoryReader",
            description: "지정된 디렉토리의 파일 및 하위 디렉토리 목록을 반환합니다. 패턴 필터링을 지원합니다 (예: *.txt, *.md).",
            type: ToolType.BuiltIn
        );

        Contract = new ToolContract(
            requiresParameters: true,
            inputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "path": { "type": "string", "description": "읽을 디렉토리 경로" },
                        "pattern": { "type": "string", "description": "파일 패턴 (선택적, 예: *.txt)" },
                        "recursive": { "type": "boolean", "description": "하위 디렉토리 포함 여부 (선택적, 기본값: false)" }
                    },
                    "required": ["path"]
                }
                """,
            outputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "DirectoryPath": { "type": "string", "description": "읽은 디렉토리 경로" },
                        "Files": { "type": "array", "items": { "type": "string" }, "description": "파일 목록" },
                        "Directories": { "type": "array", "items": { "type": "string" }, "description": "하위 디렉토리 목록" },
                        "TotalFiles": { "type": "integer", "description": "총 파일 수" },
                        "TotalDirectories": { "type": "integer", "description": "총 디렉토리 수" },
                        "Pattern": { "type": "string", "description": "적용된 패턴" }
                    },
                    "required": ["DirectoryPath", "Files", "Directories", "TotalFiles", "TotalDirectories"]
                }
                """
        );
    }

    public async Task<IToolResult> ExecuteAsync(
        object? input,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;

        var validationResult = ValidateAndExtractInput(input, startedAt);
        if (!validationResult.IsValid)
        {
            return validationResult.ErrorResult!;
        }

        try
        {
            var directoryData = await Task.Run(() => ReadDirectory(
                validationResult.DirectoryPath!,
                validationResult.Pattern,
                validationResult.Recursive), cancellationToken);

            return CreateSuccessResult(directoryData, startedAt);
        }
        catch (Exception ex)
        {
            return HandleException(ex, validationResult.DirectoryPath!, startedAt);
        }
    }

    private (bool IsValid, string? DirectoryPath, string? Pattern, bool Recursive, IToolResult? ErrorResult) ValidateAndExtractInput(
        object? input,
        DateTimeOffset startedAt)
    {
        if (!Contract.ValidateInput(input))
        {
            return (false, null, null, false, CreateFailureResult("디렉토리 경로가 필요합니다.", startedAt));
        }

        var (directoryPath, pattern, recursive) = ExtractInput(input);

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return (false, null, null, false, CreateFailureResult("올바른 디렉토리 경로가 아닙니다.", startedAt));
        }

        if (!Directory.Exists(directoryPath))
        {
            return (false, directoryPath, null, false, CreateFailureResult($"디렉토리를 찾을 수 없습니다: {directoryPath}", startedAt));
        }

        return (true, directoryPath, pattern, recursive, null);
    }

    private DirectoryReadResult ReadDirectory(string directoryPath, string? pattern, bool recursive)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = string.IsNullOrWhiteSpace(pattern)
            ? Directory.GetFiles(directoryPath, "*", searchOption)
            : Directory.GetFiles(directoryPath, pattern, searchOption);

        var directories = Directory.GetDirectories(directoryPath, "*", searchOption);

        return DirectoryReadResult.Create(directoryPath, files, directories, pattern);
    }

    private IToolResult CreateSuccessResult(DirectoryReadResult data, DateTimeOffset startedAt)
    {
        return ToolResult.Success(Metadata.Name, data, startedAt);
    }

    private IToolResult CreateFailureResult(string errorMessage, DateTimeOffset startedAt)
    {
        return ToolResult.Failure(Metadata.Name, errorMessage, startedAt);
    }

    private IToolResult HandleException(Exception ex, string directoryPath, DateTimeOffset startedAt)
    {
        var errorMessage = ex switch
        {
            UnauthorizedAccessException => $"디렉토리 접근 권한이 없습니다: {directoryPath}",
            IOException => $"디렉토리 읽기 중 오류 발생: {ex.Message}",
            _ => $"예상치 못한 오류: {ex.Message}"
        };

        return CreateFailureResult(errorMessage, startedAt);
    }

    private (string? DirectoryPath, string? Pattern, bool Recursive) ExtractInput(object? input)
    {
        if (input is Dictionary<string, object> dict)
        {
            var directoryPath = dict.ContainsKey("path") ? dict["path"]?.ToString() : null;
            var pattern = dict.ContainsKey("pattern") ? dict["pattern"]?.ToString() : null;
            var recursive = dict.ContainsKey("recursive") && bool.TryParse(dict["recursive"]?.ToString(), out var recursiveValue) && recursiveValue;

            return (directoryPath, pattern, recursive);
        }

        if (input is string path)
        {
            return (path, null, false);
        }

        return (input?.ToString(), null, false);
    }
}
