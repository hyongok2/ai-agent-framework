namespace AIAgentFramework.Tools.BuiltIn.FileWriter;

/// <summary>
/// 파일 쓰기 결과
/// </summary>
public class FileWriteResult
{
    public required string FilePath { get; init; }
    public required int BytesWritten { get; init; }
    public required int Lines { get; init; }
    public required bool FileCreated { get; init; }

    public static FileWriteResult Create(string filePath, string content, bool fileCreated)
    {
        var bytes = System.Text.Encoding.UTF8.GetByteCount(content);
        var lines = content.Split('\n').Length;

        return new FileWriteResult
        {
            FilePath = filePath,
            BytesWritten = bytes,
            Lines = lines,
            FileCreated = fileCreated
        };
    }
}
