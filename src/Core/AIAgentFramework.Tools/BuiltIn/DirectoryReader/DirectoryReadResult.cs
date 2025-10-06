namespace AIAgentFramework.Tools.BuiltIn.DirectoryReader;

/// <summary>
/// 디렉토리 읽기 결과
/// </summary>
public class DirectoryReadResult
{
    public required string DirectoryPath { get; init; }
    public required List<string> Files { get; init; }
    public required List<string> Directories { get; init; }
    public int TotalFiles { get; init; }
    public int TotalDirectories { get; init; }
    public string? Pattern { get; init; }

    public static DirectoryReadResult Create(string directoryPath, string[] files, string[] directories, string? pattern = null)
    {
        return new DirectoryReadResult
        {
            DirectoryPath = directoryPath,
            Files = files.ToList(),
            Directories = directories.ToList(),
            TotalFiles = files.Length,
            TotalDirectories = directories.Length,
            Pattern = pattern
        };
    }
}
