namespace AIAgentFramework.Tools.BuiltIn.FileReader;

/// <summary>
/// 파일 읽기 결과 데이터
/// </summary>
public class FileReadResult
{
    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 파일 내용
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 내용 길이 (문자 수)
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// 줄 수
    /// </summary>
    public int Lines { get; init; }

    public static FileReadResult Create(string filePath, string content)
    {
        return new FileReadResult
        {
            FilePath = filePath,
            Content = content,
            Length = content.Length,
            Lines = content.Split('\n').Length
        };
    }
}
