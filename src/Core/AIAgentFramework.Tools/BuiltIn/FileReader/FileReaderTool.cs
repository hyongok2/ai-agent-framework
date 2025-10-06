using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Tools.BuiltIn.FileReader;

/// <summary>
/// 파일 읽기 도구
/// 지정된 경로의 파일 내용을 읽어서 반환
/// </summary>
public class FileReaderTool : ITool
{
    public IToolMetadata Metadata { get; }
    public IToolContract Contract { get; }

    public FileReaderTool()
    {
        Metadata = new ToolMetadata(
            name: "FileReader",
            description: "지정된 경로의 파일 내용을 읽어서 반환합니다.",
            type: ToolType.BuiltIn
        );

        Contract = new ToolContract(
            requiresParameters: true,
            inputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "path": {
                            "type": "string",
                            "description": "읽을 파일의 경로 (절대 경로 또는 상대 경로)"
                        }
                    },
                    "required": ["path"]
                }
                """,
            outputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "FilePath": { "type": "string", "description": "읽은 파일의 경로" },
                        "Content": { "type": "string", "description": "파일 내용" },
                        "Length": { "type": "integer", "description": "파일 내용 길이" },
                        "Lines": { "type": "integer", "description": "파일 줄 수" }
                    },
                    "required": ["FilePath", "Content", "Length", "Lines"]
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

        var validationResult = ValidateAndExtractPath(input, startedAt);
        if (!validationResult.IsValid)
        {
            return validationResult.ErrorResult!;
        }

        try
        {
            var fileData = await ReadFileAsync(validationResult.FilePath!, cancellationToken);
            return CreateSuccessResult(fileData, startedAt);
        }
        catch (Exception ex)
        {
            return HandleException(ex, validationResult.FilePath!, startedAt);
        }
    }

    private (bool IsValid, string? FilePath, IToolResult? ErrorResult) ValidateAndExtractPath(
        object? input,
        DateTimeOffset startedAt)
    {
        if (!Contract.ValidateInput(input))
        {
            return (false, null, CreateFailureResult("파일 경로가 필요합니다.", startedAt));
        }

        var filePath = ExtractFilePath(input);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return (false, null, CreateFailureResult("올바른 파일 경로가 아닙니다.", startedAt));
        }

        if (!File.Exists(filePath))
        {
            return (false, filePath, CreateFailureResult($"파일을 찾을 수 없습니다: {filePath}", startedAt));
        }

        return (true, filePath, null);
    }

    private async Task<FileReadResult> ReadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return FileReadResult.Create(filePath, content);
    }

    private IToolResult CreateSuccessResult(FileReadResult data, DateTimeOffset startedAt)
    {
        return ToolResult.Success(Metadata.Name, data, startedAt);
    }

    private IToolResult CreateFailureResult(string errorMessage, DateTimeOffset startedAt)
    {
        return ToolResult.Failure(Metadata.Name, errorMessage, startedAt);
    }

    private IToolResult HandleException(Exception ex, string filePath, DateTimeOffset startedAt)
    {
        var errorMessage = ex switch
        {
            UnauthorizedAccessException => $"파일 접근 권한이 없습니다: {filePath}",
            IOException => $"파일 읽기 중 오류 발생: {ex.Message}",
            _ => $"예상치 못한 오류: {ex.Message}"
        };

        return CreateFailureResult(errorMessage, startedAt);
    }

    private string? ExtractFilePath(object? input)
    {
        return input switch
        {
            Dictionary<string, object> dict when dict.ContainsKey("path") => dict["path"]?.ToString(),
            string path => path,
            _ => input?.ToString()
        };
    }
}
