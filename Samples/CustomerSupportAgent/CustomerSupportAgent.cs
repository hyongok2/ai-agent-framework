using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Core.Resilience;
using AIAgentFramework.Orchestration.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Samples.CustomerSupportAgent;

/// <summary>
/// 고객 지원 AI 에이전트 샘플
/// </summary>
public class CustomerSupportAgent
{
    private readonly IOrchestrationEngine _orchestrator;
    private readonly IRegistry _registry;
    private readonly ILogger<CustomerSupportAgent> _logger;
    private readonly ResiliencePipeline _resilience;

    public CustomerSupportAgent(
        IOrchestrationEngine orchestrator,
        IRegistry registry,
        ILogger<CustomerSupportAgent> logger)
    {
        _orchestrator = orchestrator;
        _registry = registry;
        _logger = logger;
        
        // 복원력 파이프라인 구성
        _resilience = new ResiliencePipeline()
            .AddRetry(maxRetries: 3, initialDelayMs: 1000)
            .AddCircuitBreaker(failureThreshold: 5, openDurationSeconds: 60)
            .AddTimeout(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 고객 문의 처리
    /// </summary>
    public async Task<CustomerSupportResponse> HandleInquiryAsync(
        string customerInquiry,
        CustomerContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("고객 문의 처리 시작: {CustomerId}", context.CustomerId);
            
            // 문의 분류
            var classification = await ClassifyInquiryAsync(customerInquiry, cancellationToken);
            
            // 전략 선택
            var strategy = SelectStrategy(classification);
            
            // 오케스트레이션 실행
            var userRequest = new UserRequest
            {
                Content = customerInquiry,
                UserId = context.CustomerId,
                Metadata = new Dictionary<string, object>
                {
                    ["classification"] = classification,
                    ["customer_tier"] = context.CustomerTier,
                    ["history_count"] = context.PreviousInteractions.Count
                }
            };
            
            var result = await _resilience.ExecuteAsync(
                async ct => await _orchestrator.ExecuteAsync(userRequest),
                cancellationToken);
            
            // 응답 생성
            return GenerateResponse(result, classification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "고객 문의 처리 실패");
            return new CustomerSupportResponse
            {
                Success = false,
                Message = "죄송합니다. 일시적인 오류가 발생했습니다. 잠시 후 다시 시도해주세요.",
                RequiresHumanEscalation = true
            };
        }
    }

    private async Task<InquiryClassification> ClassifyInquiryAsync(
        string inquiry,
        CancellationToken cancellationToken)
    {
        var classifier = _registry.GetLLMFunction("classifier");
        if (classifier == null)
        {
            _logger.LogWarning("Classifier not found, using default classification");
            return new InquiryClassification { Type = "general", Urgency = "normal" };
        }
        
        var context = new LLMContext
        {
            UserRequest = inquiry,
            Parameters = new Dictionary<string, object>
            {
                ["instruction"] = "고객 문의를 분류하세요: billing, technical, general, complaint",
                ["inquiry"] = inquiry
            }
        };
        
        var result = await classifier.ExecuteAsync(context, cancellationToken);
        
        // 결과 파싱 (실제로는 JSON 파싱)
        return new InquiryClassification
        {
            Type = ExtractType(result.Content),
            Urgency = ExtractUrgency(result.Content),
            Intent = ExtractIntent(result.Content)
        };
    }

    private IOrchestrationStrategy SelectStrategy(InquiryClassification classification)
    {
        // 문의 유형에 따른 전략 선택
        return classification.Type switch
        {
            "complaint" => new ReActStrategy(_registry, _logger.CreateLogger<ReActStrategy>()),
            "technical" => new PlanExecuteStrategy(_registry, _logger.CreateLogger<PlanExecuteStrategy>()),
            _ => new PlanExecuteStrategy(_registry, _logger.CreateLogger<PlanExecuteStrategy>())
        };
    }

    private CustomerSupportResponse GenerateResponse(
        IOrchestrationResult result,
        InquiryClassification classification)
    {
        var response = new CustomerSupportResponse
        {
            Success = result.IsSuccess,
            Classification = classification,
            SessionId = result.Context.SessionId,
            ExecutionSteps = result.Context.ExecutionHistory.Count
        };
        
        // SharedData에서 최종 답변 추출
        if (result.Context.SharedData.TryGetValue("final_answer", out var answer))
        {
            response.Message = answer?.ToString() ?? "답변을 생성할 수 없습니다.";
        }
        else
        {
            response.Message = "문의를 처리했습니다. 자세한 내용은 담당자에게 문의해주세요.";
            response.RequiresHumanEscalation = true;
        }
        
        // 긴급도가 높으면 에스컬레이션
        if (classification.Urgency == "high" || classification.Type == "complaint")
        {
            response.RequiresHumanEscalation = true;
        }
        
        return response;
    }

    private string ExtractType(string content)
    {
        // 실제로는 JSON 파싱 또는 정규식 사용
        if (content.Contains("billing")) return "billing";
        if (content.Contains("technical")) return "technical";
        if (content.Contains("complaint")) return "complaint";
        return "general";
    }

    private string ExtractUrgency(string content)
    {
        if (content.Contains("urgent") || content.Contains("high")) return "high";
        if (content.Contains("low")) return "low";
        return "normal";
    }

    private string ExtractIntent(string content)
    {
        // 의도 추출 로직
        return "inquiry";
    }
}

/// <summary>
/// 고객 컨텍스트
/// </summary>
public class CustomerContext
{
    public string CustomerId { get; set; } = "";
    public string CustomerTier { get; set; } = "standard";
    public List<string> PreviousInteractions { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 문의 분류
/// </summary>
public class InquiryClassification
{
    public string Type { get; set; } = "";
    public string Urgency { get; set; } = "";
    public string Intent { get; set; } = "";
}

/// <summary>
/// 고객 지원 응답
/// </summary>
public class CustomerSupportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public InquiryClassification? Classification { get; set; }
    public bool RequiresHumanEscalation { get; set; }
    public string SessionId { get; set; } = "";
    public int ExecutionSteps { get; set; }
}

/// <summary>
/// 서비스 등록 확장
/// </summary>
public static class CustomerSupportServiceExtensions
{
    public static IServiceCollection AddCustomerSupportAgent(
        this IServiceCollection services)
    {
        services.AddSingleton<CustomerSupportAgent>();
        
        // 필요한 LLM 기능 등록
        services.AddSingleton<ILLMFunction>(sp =>
            new ClassifierFunction(sp.GetRequiredService<ILLMProvider>()));
        
        services.AddSingleton<ILLMFunction>(sp =>
            new SentimentAnalyzer(sp.GetRequiredService<ILLMProvider>()));
        
        // 필요한 도구 등록
        services.AddSingleton<ITool>(sp =>
            new CustomerDatabaseTool(sp.GetRequiredService<ILogger<CustomerDatabaseTool>>()));
        
        services.AddSingleton<ITool>(sp =>
            new TicketSystemTool(sp.GetRequiredService<ILogger<TicketSystemTool>>()));
        
        return services;
    }
}

// 샘플 LLM 기능들
public class ClassifierFunction : ILLMFunction
{
    private readonly ILLMProvider _provider;
    
    public ClassifierFunction(ILLMProvider provider)
    {
        _provider = provider;
    }
    
    public string Name => "classifier";
    public string Description => "문의 분류기";
    public FunctionCategory Category => FunctionCategory.Analyzer;
    
    public async Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        // 실제 LLM 호출
        var prompt = $"다음 고객 문의를 분류하세요:\n{context.UserRequest}\n\n카테고리: billing, technical, general, complaint";
        var response = await _provider.GenerateAsync(prompt, null, cancellationToken);
        
        return new LLMResult
        {
            Success = true,
            Content = response.Content,
            Model = response.Model,
            TokensUsed = response.TokensUsed
        };
    }
}

public class SentimentAnalyzer : ILLMFunction
{
    private readonly ILLMProvider _provider;
    
    public SentimentAnalyzer(ILLMProvider provider)
    {
        _provider = provider;
    }
    
    public string Name => "sentiment_analyzer";
    public string Description => "감정 분석기";
    public FunctionCategory Category => FunctionCategory.Analyzer;
    
    public async Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        var prompt = $"다음 텍스트의 감정을 분석하세요:\n{context.UserRequest}\n\n감정: positive, negative, neutral";
        var response = await _provider.GenerateAsync(prompt, null, cancellationToken);
        
        return new LLMResult
        {
            Success = true,
            Content = response.Content,
            Model = response.Model,
            TokensUsed = response.TokensUsed
        };
    }
}

// 샘플 도구들
public class CustomerDatabaseTool : ToolBase
{
    public CustomerDatabaseTool(ILogger<CustomerDatabaseTool> logger) : base(logger) { }
    
    public override string Name => "customer_database";
    public override string Description => "고객 데이터베이스 조회";
    public override string Category => "Database";
    public override IToolContract Contract => new ToolContract
    {
        RequiredParameters = new[] { "customer_id" },
        OptionalParameters = new[] { "fields" }
    };
    
    protected override async Task<ToolResult> ExecuteInternalAsync(
        IToolInput input,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetRequiredParameter<string>(input, "customer_id");
        
        // 실제로는 데이터베이스 조회
        await Task.Delay(100, cancellationToken);
        
        var customerData = new
        {
            id = customerId,
            name = "John Doe",
            tier = "premium",
            since = "2020-01-01",
            total_purchases = 15000
        };
        
        return ToolResult.CreateSuccess(customerData);
    }
}

public class TicketSystemTool : ToolBase
{
    public TicketSystemTool(ILogger<TicketSystemTool> logger) : base(logger) { }
    
    public override string Name => "ticket_system";
    public override string Description => "티켓 시스템 연동";
    public override string Category => "Integration";
    public override IToolContract Contract => new ToolContract
    {
        RequiredParameters = new[] { "action" },
        OptionalParameters = new[] { "ticket_id", "details" }
    };
    
    protected override async Task<ToolResult> ExecuteInternalAsync(
        IToolInput input,
        CancellationToken cancellationToken = default)
    {
        var action = GetRequiredParameter<string>(input, "action");
        
        await Task.Delay(100, cancellationToken);
        
        return action switch
        {
            "create" => ToolResult.CreateSuccess(new { ticket_id = Guid.NewGuid().ToString(), status = "created" }),
            "update" => ToolResult.CreateSuccess(new { success = true, status = "updated" }),
            "query" => ToolResult.CreateSuccess(new { tickets = new[] { "TICKET-001", "TICKET-002" } }),
            _ => ToolResult.CreateFailure($"Unknown action: {action}")
        };
    }
}