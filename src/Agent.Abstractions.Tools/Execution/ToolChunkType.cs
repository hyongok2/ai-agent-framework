namespace Agent.Abstractions.Tools.Execution;

/// <summary>
/// 도구 청크 타입
/// </summary>
public enum ToolChunkType
{
    /// <summary>상태 업데이트</summary>
    Status,
    
    /// <summary>진행률</summary>
    Progress,
    
    /// <summary>텍스트 출력</summary>
    Text,
    
    /// <summary>구조화된 데이터</summary>
    Data,
    
    /// <summary>부분 결과</summary>
    Partial,
    
    /// <summary>최종 결과</summary>
    Final,
    
    /// <summary>에러</summary>
    Error
}