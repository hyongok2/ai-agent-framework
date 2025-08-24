namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 응답 형식
/// </summary>
public enum ResponseFormat
{
    /// <summary>일반 텍스트</summary>
    Text,
    
    /// <summary>JSON</summary>
    Json,
    
    /// <summary>마크다운</summary>
    Markdown,
    
    /// <summary>HTML</summary>
    Html,
    
    /// <summary>XML</summary>
    Xml
}