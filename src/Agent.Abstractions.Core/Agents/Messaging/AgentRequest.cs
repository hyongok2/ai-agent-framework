using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agent.Abstractions.Core.Common.Identifiers;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에이전트 요청 모델
/// </summary>
public sealed record AgentRequest
{
    /// <summary>
    /// 요청 ID
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// 사용자 메시지
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 대화 ID (대화 컨텍스트 유지용)
    /// </summary>
    public string? ConversationId { get; init; }
    
    /// <summary>
    /// 사용자 ID
    /// </summary>
    public string? UserId { get; init; }
    
    /// <summary>
    /// 세션 ID
    /// </summary>
    public string? SessionId { get; init; }
    
    /// <summary>
    /// 요청 옵션
    /// </summary>
    public RequestOptions Options { get; init; } = new();
    
    /// <summary>
    /// 실행 컨텍스트
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
    
    /// <summary>
    /// 첨부 파일
    /// </summary>
    public List<Attachment> Attachments { get; init; } = new();
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// 요청 생성 시간
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 요청 만료 시간
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
    
    /// <summary>
    /// 우선순위 (0-10, 높을수록 우선)
    /// </summary>
    public int Priority { get; init; } = 5;
    
    /// <summary>
    /// 요청 소스 (예: "web", "api", "cli")
    /// </summary>
    public string? Source { get; init; }
    
    /// <summary>
    /// 기본 요청 생성
    /// </summary>
    public static AgentRequest Create(string message)
        => new() { Message = message };
    
    /// <summary>
    /// 대화 컨텍스트를 포함한 요청 생성
    /// </summary>
    public static AgentRequest CreateWithConversation(string message, string conversationId)
        => new() 
        { 
            Message = message,
            ConversationId = conversationId
        };
    
    /// <summary>
    /// 첨부 파일이 있는 요청 생성
    /// </summary>
    public static AgentRequest CreateWithAttachments(string message, params Attachment[] attachments)
        => new()
        {
            Message = message,
            Attachments = new List<Attachment>(attachments)
        };
    
    /// <summary>
    /// 요청 검증
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(Message))
            errors.Add("Message cannot be empty");
        
        if (Message?.Length > Options.MaxMessageLength)
            errors.Add($"Message exceeds maximum length of {Options.MaxMessageLength}");
        
        if (Attachments.Count > Options.MaxAttachments)
            errors.Add($"Too many attachments (max: {Options.MaxAttachments})");
        
        foreach (var attachment in Attachments)
        {
            if (attachment.Size > Options.MaxAttachmentSize)
                errors.Add($"Attachment '{attachment.Name}' exceeds maximum size");
        }
        
        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow)
            errors.Add("Request has already expired");
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
    
    /// <summary>
    /// JSON으로 직렬화
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(this, options ?? JsonOptions.Default);
    
    /// <summary>
    /// JSON에서 역직렬화
    /// </summary>
    public static AgentRequest FromJson(string json, JsonSerializerOptions? options = null)
        => JsonSerializer.Deserialize<AgentRequest>(json, options ?? JsonOptions.Default)
           ?? throw new InvalidOperationException("Failed to deserialize AgentRequest");
    
    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}