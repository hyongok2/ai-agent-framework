using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Tools.BuiltIn.DirectoryCreator;

/// <summary>
/// 디렉토리 생성 도구
/// 지정된 경로에 디렉토리를 생성합니다
/// </summary>
public class DirectoryCreatorTool : ITool
{
    public IToolMetadata Metadata { get; }
    public IToolContract Contract { get; }

    public DirectoryCreatorTool()
    {
        Metadata = new ToolMetadata(
            name: "DirectoryCreator",
            description: "지정된 경로에 디렉토리를 생성합니다. 중간 경로도 자동으로 생성됩니다.",
            type: ToolType.BuiltIn
        );

        Contract = new ToolContract(
            requiresParameters: true,
            inputSchema: """
                {
                    "type": "string",
                    "description": "생성할 디렉토리 경로"
                }
                """,
            outputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "DirectoryPath": { "type": "string", "description": "생성된 디렉토리 경로" },
                        "Created": { "type": "boolean", "description": "새로 생성 여부" },
                        "AlreadyExisted": { "type": "boolean", "description": "이미 존재했는지 여부" }
                    },
                    "required": ["DirectoryPath", "Created", "AlreadyExisted"]
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
            var result = await Task.Run(() => CreateDirectory(validationResult.DirectoryPath!), cancellationToken);
            return CreateSuccessResult(result, startedAt);
        }
        catch (Exception ex)
        {
            return HandleException(ex, validationResult.DirectoryPath!, startedAt);
        }
    }

    private (bool IsValid, string? DirectoryPath, IToolResult? ErrorResult) ValidateAndExtractPath(
        object? input,
        DateTimeOffset startedAt)
    {
        if (!Contract.ValidateInput(input))
        {
            return (false, null, CreateFailureResult("디렉토리 경로가 필요합니다.", startedAt));
        }

        var directoryPath = ExtractDirectoryPath(input);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return (false, null, CreateFailureResult("올바른 디렉토리 경로가 아닙니다.", startedAt));
        }

        return (true, directoryPath, null);
    }

    private DirectoryCreateResult CreateDirectory(string directoryPath)
    {
        var alreadyExisted = Directory.Exists(directoryPath);

        if (!alreadyExisted)
        {
            Directory.CreateDirectory(directoryPath);
            return DirectoryCreateResult.Create(directoryPath, true, false);
        }

        return DirectoryCreateResult.Create(directoryPath, false, true);
    }

    private IToolResult CreateSuccessResult(DirectoryCreateResult data, DateTimeOffset startedAt)
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
            UnauthorizedAccessException => $"디렉토리 생성 권한이 없습니다: {directoryPath}",
            IOException => $"디렉토리 생성 중 오류 발생: {ex.Message}",
            _ => $"예상치 못한 오류: {ex.Message}"
        };

        return CreateFailureResult(errorMessage, startedAt);
    }

    private string? ExtractDirectoryPath(object? input)
    {
        return input switch
        {
            string path => path,
            _ => input?.ToString()
        };
    }
}
