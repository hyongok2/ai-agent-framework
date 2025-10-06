namespace AIAgentFramework.Tools.BuiltIn.DirectoryCreator;

/// <summary>
/// 디렉토리 생성 결과
/// </summary>
public class DirectoryCreateResult
{
    public required string DirectoryPath { get; init; }
    public required bool Created { get; init; }
    public required bool AlreadyExisted { get; init; }

    public static DirectoryCreateResult Create(string directoryPath, bool created, bool alreadyExisted)
    {
        return new DirectoryCreateResult
        {
            DirectoryPath = directoryPath,
            Created = created,
            AlreadyExisted = alreadyExisted
        };
    }
}
