namespace Agent.Abstractions.Core.Agents;
/// <summary>
/// 첨부 파일
/// </summary>
public sealed record Attachment
{
    /// <summary>
    /// 파일 이름
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// MIME 타입
    /// </summary>
    public required string MimeType { get; init; }
    
    /// <summary>
    /// 파일 크기 (바이트)
    /// </summary>
    public long Size { get; init; }
    
    /// <summary>
    /// 파일 데이터 (Base64 또는 URL)
    /// </summary>
    public required string Data { get; init; }
    
    /// <summary>
    /// 데이터 타입
    /// </summary>
    public AttachmentDataType DataType { get; init; } = AttachmentDataType.Base64;
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// 파일에서 첨부 파일 생성
    /// </summary>
    public static Attachment FromFile(string filePath)
    {
        var fileInfo = new System.IO.FileInfo(filePath);
        var bytes = System.IO.File.ReadAllBytes(filePath);
        
        return new Attachment
        {
            Name = fileInfo.Name,
            MimeType = GetMimeType(fileInfo.Extension),
            Size = fileInfo.Length,
            Data = Convert.ToBase64String(bytes),
            DataType = AttachmentDataType.Base64
        };
    }
    
    /// <summary>
    /// URL에서 첨부 파일 생성
    /// </summary>
    public static Attachment FromUrl(string url, string name, string mimeType)
        => new()
        {
            Name = name,
            MimeType = mimeType,
            Data = url,
            DataType = AttachmentDataType.Url,
            Size = 0 // URL의 경우 크기를 미리 알 수 없음
        };
    
    private static string GetMimeType(string extension)
        => extension.ToLowerInvariant() switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".json" => "application/json",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".csv" => "text/csv",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
}