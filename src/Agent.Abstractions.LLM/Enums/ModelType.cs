namespace Agent.Abstractions.LLM.Enums;

/// <summary>
/// 모델 타입
/// </summary>
public enum ModelType
{
    /// <summary>텍스트 생성</summary>
    TextGeneration,
    
    /// <summary>임베딩</summary>
    Embedding,
    
    /// <summary>이미지 생성</summary>
    ImageGeneration,
    
    /// <summary>멀티모달</summary>
    Multimodal,
    
    /// <summary>코드 생성</summary>
    CodeGeneration,
    
    /// <summary>채팅</summary>
    Chat
}