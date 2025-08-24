namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 첨부 파일 데이터 타입
/// </summary>
public enum AttachmentDataType
{
    /// <summary>Base64 인코딩된 데이터</summary>
    Base64,
    
    /// <summary>URL 참조</summary>
    Url,
    
    /// <summary>파일 경로</summary>
    FilePath,
    
    /// <summary>인라인 텍스트</summary>
    Text
}