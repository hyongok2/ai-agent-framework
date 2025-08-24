namespace Agent.Abstractions.Core.Schema;

/// <summary>
/// 스키마 관련 상수
/// </summary>
public static class SchemaConstants
{
    /// <summary>
    /// 코어 스키마 네임스페이스
    /// </summary>
    public const string CoreNamespace = "agent.core";
    
    /// <summary>
    /// 도구 스키마 네임스페이스
    /// </summary>
    public const string ToolsNamespace = "agent.tools";
    
    /// <summary>
    /// LLM 스키마 네임스페이스
    /// </summary>
    public const string LlmNamespace = "agent.llm";
    
    /// <summary>
    /// 오케스트레이션 스키마 네임스페이스
    /// </summary>
    public const string OrchestrationNamespace = "agent.orchestration";
    
    // 코어 스키마 ID
    public const string StepSchemaId = $"{OrchestrationNamespace}.step";
    public const string PlanSchemaId = $"{OrchestrationNamespace}.plan";
    public const string StreamChunkSchemaId = $"{CoreNamespace}.stream-chunk";
    
    // 도구 스키마 ID
    public const string ToolDescriptorSchemaId = $"{ToolsNamespace}.descriptor";
    public const string ToolResultSchemaId = $"{ToolsNamespace}.result";
    public const string ToolInputSchemaId = $"{ToolsNamespace}.input";
    
    // LLM 스키마 ID
    public const string LlmRequestSchemaId = $"{LlmNamespace}.request";
    public const string LlmResponseSchemaId = $"{LlmNamespace}.response";
    public const string PromptSchemaId = $"{LlmNamespace}.prompt";
    
    /// <summary>
    /// JSON Schema 버전
    /// </summary>
    public const string JsonSchemaVersion = "https://json-schema.org/draft/2020-12/schema";
    
    /// <summary>
    /// 기본 스키마 버전
    /// </summary>
    public const string DefaultSchemaVersion = "1.0.0";
}