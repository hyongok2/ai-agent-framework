using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Tools.Models;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Tools.BuiltIn.System;

/// <summary>
/// 파일 시스템 도구 - 파일 및 디렉터리 작업 제공
/// </summary>
public class FileSystemTool : ToolBase
{
    private readonly FileSystemOptions _options;

    /// <summary>
    /// 파일 시스템 옵션
    /// </summary>
    public class FileSystemOptions
    {
        public string RootPath { get; set; } = Directory.GetCurrentDirectory();
        public bool AllowWrite { get; set; } = false;
        public bool AllowDelete { get; set; } = false;
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
        public List<string> AllowedPaths { get; set; } = new();
        public List<string> DeniedExtensions { get; set; } = new() { ".exe", ".dll", ".bat", ".cmd", ".ps1" };
    }

    /// <summary>
    /// 생성자
    /// </summary>
    public FileSystemTool(ILogger<FileSystemTool> logger, FileSystemOptions? options = null)
        : base(logger)
    {
        _options = options ?? new FileSystemOptions();
    }

    /// <inheritdoc />
    public override string Name => "FileSystem";

    /// <inheritdoc />
    public override string Description => "파일 시스템 작업을 수행합니다";

    /// <inheritdoc />
    public override IToolContract Contract => new ToolContract
    {
        RequiredParameters = new List<string> { "operation" },
        OptionalParameters = new List<string> { "path", "content", "overwrite", "recursive" }
    };

    /// <inheritdoc />
    protected override async Task<ToolResult> ExecuteInternalAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!input.Parameters.TryGetValue("operation", out var operationObj))
            {
                return ToolResult.CreateFailure("작업 타입이 제공되지 않았습니다");
            }

            var operation = operationObj?.ToString()?.ToLowerInvariant();

            return operation switch
            {
                "read" => await ReadFileAsync(input, cancellationToken),
                "write" => await WriteFileAsync(input, cancellationToken),
                "list" => await ListDirectoryAsync(input, cancellationToken),
                "delete" => await DeleteAsync(input, cancellationToken),
                "info" => await GetInfoAsync(input, cancellationToken),
                _ => ToolResult.CreateFailure($"지원되지 않는 작업: {operation}")
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult.CreateFailure($"접근 권한 오류: {ex.Message}");
        }
        catch (IOException ex)
        {
            return ToolResult.CreateFailure($"I/O 오류: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ToolResult.CreateFailure($"작업 실패: {ex.Message}");
        }
    }

    private async Task<ToolResult> ReadFileAsync(IToolInput input, CancellationToken cancellationToken)
    {
        if (!input.Parameters.TryGetValue("path", out var pathObj))
        {
            return ToolResult.CreateFailure("파일 경로가 제공되지 않았습니다");
        }

        var path = pathObj?.ToString() ?? string.Empty;
        if (!File.Exists(path))
        {
            return ToolResult.CreateFailure($"파일을 찾을 수 없습니다: {path}");
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var fileInfo = new FileInfo(path);

        return ToolResult.CreateSuccess(new Dictionary<string, object>
        {
            ["Path"] = path,
            ["Content"] = content,
            ["Size"] = fileInfo.Length,
            ["LastModified"] = fileInfo.LastWriteTimeUtc
        });
    }

    private async Task<ToolResult> WriteFileAsync(IToolInput input, CancellationToken cancellationToken)
    {
        if (!_options.AllowWrite)
        {
            return ToolResult.CreateFailure("파일 쓰기가 허용되지 않습니다");
        }

        if (!input.Parameters.TryGetValue("path", out var pathObj) ||
            !input.Parameters.TryGetValue("content", out var contentObj))
        {
            return ToolResult.CreateFailure("path와 content 파라미터가 필요합니다");
        }

        var path = pathObj?.ToString() ?? string.Empty;
        var content = contentObj?.ToString() ?? string.Empty;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken);

        return ToolResult.CreateSuccess(new Dictionary<string, object>
        {
            ["Path"] = path,
            ["Size"] = content.Length,
            ["Created"] = DateTime.UtcNow
        });
    }

    private async Task<ToolResult> ListDirectoryAsync(IToolInput input, CancellationToken cancellationToken)
    {
        if (!input.Parameters.TryGetValue("path", out var pathObj))
        {
            return ToolResult.CreateFailure("디렉터리 경로가 제공되지 않았습니다");
        }

        var path = pathObj?.ToString() ?? string.Empty;
        if (!Directory.Exists(path))
        {
            return ToolResult.CreateFailure($"디렉터리를 찾을 수 없습니다: {path}");
        }

        var files = Directory.GetFiles(path).Select(f => new
        {
            Type = "File",
            Name = Path.GetFileName(f),
            Path = f,
            Size = new FileInfo(f).Length
        });

        var directories = Directory.GetDirectories(path).Select(d => new
        {
            Type = "Directory", 
            Name = Path.GetFileName(d),
            Path = d,
            Size = (long?)null
        });

        var items = files.Cast<object>().Concat(directories).ToList();

        return await Task.FromResult(ToolResult.CreateSuccess(new Dictionary<string, object>
        {
            ["Path"] = path,
            ["ItemCount"] = items.Count,
            ["Items"] = items
        }));
    }

    private async Task<ToolResult> DeleteAsync(IToolInput input, CancellationToken cancellationToken)
    {
        if (!_options.AllowDelete)
        {
            return ToolResult.CreateFailure("파일 삭제가 허용되지 않습니다");
        }

        if (!input.Parameters.TryGetValue("path", out var pathObj))
        {
            return ToolResult.CreateFailure("경로가 제공되지 않았습니다");
        }

        var path = pathObj?.ToString() ?? string.Empty;

        if (File.Exists(path))
        {
            File.Delete(path);
        }
        else if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        else
        {
            return ToolResult.CreateFailure($"경로를 찾을 수 없습니다: {path}");
        }

        return await Task.FromResult(ToolResult.CreateSuccess(new Dictionary<string, object>
        {
            ["Path"] = path,
            ["Deleted"] = true,
            ["DeletedAt"] = DateTime.UtcNow
        }));
    }

    private async Task<ToolResult> GetInfoAsync(IToolInput input, CancellationToken cancellationToken)
    {
        if (!input.Parameters.TryGetValue("path", out var pathObj))
        {
            return ToolResult.CreateFailure("경로가 제공되지 않았습니다");
        }

        var path = pathObj?.ToString() ?? string.Empty;

        if (File.Exists(path))
        {
            var fileInfo = new FileInfo(path);
            return await Task.FromResult(ToolResult.CreateSuccess(new Dictionary<string, object>
            {
                ["Type"] = "File",
                ["Path"] = fileInfo.FullName,
                ["Name"] = fileInfo.Name,
                ["Size"] = fileInfo.Length,
                ["Created"] = fileInfo.CreationTimeUtc,
                ["Modified"] = fileInfo.LastWriteTimeUtc
            }));
        }
        else if (Directory.Exists(path))
        {
            var dirInfo = new DirectoryInfo(path);
            return await Task.FromResult(ToolResult.CreateSuccess(new Dictionary<string, object>
            {
                ["Type"] = "Directory",
                ["Path"] = dirInfo.FullName,
                ["Name"] = dirInfo.Name,
                ["Created"] = dirInfo.CreationTimeUtc,
                ["Modified"] = dirInfo.LastWriteTimeUtc
            }));
        }
        else
        {
            return ToolResult.CreateFailure($"경로를 찾을 수 없습니다: {path}");
        }
    }
}