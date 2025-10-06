using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Tools.BuiltIn.FileWriter;

/// <summary>
/// 파일 쓰기 도구
/// 지정된 경로에 파일을 생성하거나 내용을 씁니다
/// </summary>
public class FileWriterTool : ITool
{
    public IToolMetadata Metadata { get; }
    public IToolContract Contract { get; }

    public FileWriterTool()
    {
        Metadata = new ToolMetadata(
            name: "FileWriter",
            description: "지정된 경로에 파일을 생성하거나 내용을 씁니다. 기존 파일이 있으면 덮어씁니다.",
            type: ToolType.BuiltIn
        );

        Contract = new ToolContract(
            requiresParameters: true,
            inputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "path": { "type": "string", "description": "저장할 파일 경로" },
                        "content": { "type": "string", "description": "파일에 쓸 내용" },
                        "append": { "type": "boolean", "description": "기존 파일에 추가 여부 (선택적, 기본값: false)" }
                    },
                    "required": ["path", "content"]
                }
                """,
            outputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "FilePath": { "type": "string", "description": "작성된 파일 경로" },
                        "BytesWritten": { "type": "integer", "description": "작성된 바이트 수" },
                        "Lines": { "type": "integer", "description": "작성된 줄 수" },
                        "FileCreated": { "type": "boolean", "description": "새 파일 생성 여부" }
                    },
                    "required": ["FilePath", "BytesWritten", "Lines", "FileCreated"]
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
            var fileWriteResult = await WriteFileAsync(
                validationResult.FilePath!,
                validationResult.Content!,
                validationResult.Append,
                cancellationToken);

            return CreateSuccessResult(fileWriteResult, startedAt);
        }
        catch (Exception ex)
        {
            return HandleException(ex, validationResult.FilePath!, startedAt);
        }
    }

    private (bool IsValid, string? FilePath, string? Content, bool Append, IToolResult? ErrorResult) ValidateAndExtractInput(
        object? input,
        DateTimeOffset startedAt)
    {
        if (!Contract.ValidateInput(input))
        {
            return (false, null, null, false, CreateFailureResult("파일 경로와 내용이 필요합니다.", startedAt));
        }

        var (filePath, content, append) = ExtractInput(input);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return (false, null, null, false, CreateFailureResult("올바른 파일 경로가 아닙니다.", startedAt));
        }

        if (content == null)
        {
            return (false, filePath, null, false, CreateFailureResult("파일에 쓸 내용이 필요합니다.", startedAt));
        }

        // 디렉토리가 존재하지 않으면 생성
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                return (false, filePath, content, false,
                    CreateFailureResult($"디렉토리 생성 실패: {ex.Message}", startedAt));
            }
        }

        return (true, filePath, content, append, null);
    }

    private async Task<FileWriteResult> WriteFileAsync(
        string filePath,
        string content,
        bool append,
        CancellationToken cancellationToken)
    {
        var fileCreated = !File.Exists(filePath);

        if (append)
        {
            await File.AppendAllTextAsync(filePath, content, cancellationToken);
        }
        else
        {
            await File.WriteAllTextAsync(filePath, content, cancellationToken);
        }

        return FileWriteResult.Create(filePath, content, fileCreated);
    }

    private IToolResult CreateSuccessResult(FileWriteResult data, DateTimeOffset startedAt)
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
            UnauthorizedAccessException => $"파일 쓰기 권한이 없습니다: {filePath}",
            IOException => $"파일 쓰기 중 오류 발생: {ex.Message}",
            _ => $"예상치 못한 오류: {ex.Message}"
        };

        return CreateFailureResult(errorMessage, startedAt);
    }

    private (string? FilePath, string? Content, bool Append) ExtractInput(object? input)
    {
        if (input is Dictionary<string, object> dict)
        {
            var filePath = dict.ContainsKey("path") ? dict["path"]?.ToString() : null;
            var content = dict.ContainsKey("content") ? dict["content"]?.ToString() : null;
            var append = dict.ContainsKey("append") && bool.TryParse(dict["append"]?.ToString(), out var appendValue) && appendValue;

            return (filePath, content, append);
        }

        return (null, null, false);
    }
}
