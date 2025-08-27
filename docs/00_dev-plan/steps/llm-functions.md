# LLM Functions 상세 설계

## 개요
14가지 LLM 기능의 상세 설계 및 구현 가이드

## Base Class 설계

```csharp
public abstract class BaseLLMFunction : ILLMFunction
{
    // 기본 속성
    public abstract string Role { get; }
    public abstract string Description { get; }
    public virtual int Priority { get; } = 100;
    
    // 템플릿 메서드 패턴
    public async Task<ILLMResult> ExecuteAsync(ILLMContext context)
    {
        // 1. 프롬프트 준비
        var prompt = await PreparePromptAsync(context);
        
        // 2. LLM 호출
        var response = await CallLLMAsync(prompt, context);
        
        // 3. 응답 파싱
        var parsed = await ParseResponseAsync(response);
        
        // 4. 검증
        var validated = await ValidateResponseAsync(parsed);
        
        // 5. 후처리
        return await PostProcessAsync(validated, context);
    }
    
    // 확장 포인트
    protected abstract Task<Prompt> PreparePromptAsync(ILLMContext context);
    protected abstract Task<ParsedResponse> ParseResponseAsync(string response);
    protected virtual Task<ParsedResponse> ValidateResponseAsync(ParsedResponse response) => Task.FromResult(response);
    protected virtual Task<ILLMResult> PostProcessAsync(ParsedResponse response, ILLMContext context) => Task.FromResult(response);
}
```

---

## 1. PlannerFunction (계획 수립자)

### 역할
사용자 요구사항을 분석하고 실행 가능한 단계별 계획 수립

### 입출력 구조
```csharp
public class PlannerInput
{
    public string UserRequest { get; set; }
    public List<ToolInfo> AvailableTools { get; set; }
    public List<LLMFunctionInfo> AvailableFunctions { get; set; }
    public ExecutionContext Context { get; set; }
    public List<string> Constraints { get; set; }
}

public class PlannerOutput
{
    public string PlanId { get; set; }
    public string Summary { get; set; }
    public List<PlannedStep> Steps { get; set; }
    public Dictionary<string, object> Variables { get; set; }
    public EstimatedMetrics Metrics { get; set; }
}

public class PlannedStep
{
    public int Order { get; set; }
    public string Type { get; set; } // Tool | LLMFunction | Response
    public string Name { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public List<int> DependsOn { get; set; }
    public string Purpose { get; set; }
    public bool IsOptional { get; set; }
}
```

### 프롬프트 전략
```markdown
# 계획 수립 규칙
1. 최소 단계로 목표 달성
2. 의존성 명확히 정의
3. 실패 가능성 있는 단계 표시
4. 예상 시간/비용 추정
```

---

## 2. AnalyzerFunction (분석가)

### 역할
입력 데이터를 분석하여 구조화된 정보 추출

### 입출력 구조
```csharp
public class AnalyzerInput
{
    public string Content { get; set; }
    public AnalysisType Type { get; set; } // Text | Code | Data | Log
    public List<string> FocusAreas { get; set; }
    public OutputFormat Format { get; set; }
}

public class AnalyzerOutput
{
    public Summary Summary { get; set; }
    public List<KeyPoint> KeyPoints { get; set; }
    public Dictionary<string, object> ExtractedData { get; set; }
    public List<Pattern> Patterns { get; set; }
    public SentimentAnalysis Sentiment { get; set; }
    public List<Anomaly> Anomalies { get; set; }
    public ConfidenceScore Confidence { get; set; }
}
```

### 특화 기능
- 다양한 데이터 타입 지원
- 패턴 인식
- 이상 탐지
- 감정 분석

---

## 3. GeneratorFunction (생성자)

### 역할
요구사항에 맞는 새로운 콘텐츠 생성

### 입출력 구조
```csharp
public class GeneratorInput
{
    public string Requirements { get; set; }
    public ContentType Type { get; set; } // Text | Code | JSON | Markdown
    public StyleParameters Style { get; set; }
    public LengthConstraints Length { get; set; }
    public List<string> MustInclude { get; set; }
    public List<string> MustAvoid { get; set; }
}

public class GeneratorOutput
{
    public string Content { get; set; }
    public ContentMetadata Metadata { get; set; }
    public List<string> Warnings { get; set; }
    public QualityMetrics Quality { get; set; }
}
```

### 생성 전략
- Temperature 조절로 창의성 제어
- 반복 생성 및 최선 선택
- 단계적 개선

---

## 4. SummarizerFunction (요약자)

### 역할
긴 텍스트를 핵심만 추출하여 요약

### 입출력 구조
```csharp
public class SummarizerInput
{
    public string Content { get; set; }
    public SummaryType Type { get; set; } // Executive | Technical | Bullet | Narrative
    public int MaxLength { get; set; }
    public List<string> FocusTopics { get; set; }
    public bool PreserveDetails { get; set; }
}

public class SummarizerOutput
{
    public string Summary { get; set; }
    public List<string> KeyPoints { get; set; }
    public List<string> OmittedDetails { get; set; }
    public CompressionRatio Ratio { get; set; }
}
```

### 요약 기법
- 추출적 요약 + 생성적 요약
- 계층적 요약 (긴 문서)
- 관점별 요약

---

## 5. ToolParameterSetterFunction (도구 파라미터 설정자)

### 역할
Tool 실행에 필요한 파라미터를 사용자 의도에서 추출/생성

### 입출력 구조
```csharp
public class ParameterSetterInput
{
    public string UserIntent { get; set; }
    public ToolContract ToolContract { get; set; }
    public Dictionary<string, object> Context { get; set; }
    public List<ParameterExample> Examples { get; set; }
}

public class ParameterSetterOutput
{
    public Dictionary<string, object> Parameters { get; set; }
    public List<ParameterMapping> Mappings { get; set; }
    public List<Assumption> Assumptions { get; set; }
    public ValidationResult Validation { get; set; }
}
```

### 핵심 기능
- JSON Schema 기반 파라미터 생성
- 타입 변환 및 검증
- 기본값 및 선택값 처리

---

## 6. EvaluatorFunction (평가자)

### 역할
결과물의 품질을 평가하고 개선점 제시

### 입출력 구조
```csharp
public class EvaluatorInput
{
    public string Content { get; set; }
    public List<EvaluationCriteria> Criteria { get; set; }
    public string ExpectedQuality { get; set; }
    public bool ProvideImprovement { get; set; }
}

public class EvaluatorOutput
{
    public OverallScore Score { get; set; }
    public Dictionary<string, Score> CriteriaScores { get; set; }
    public List<Issue> Issues { get; set; }
    public List<Improvement> Improvements { get; set; }
    public string Recommendation { get; set; }
}
```

---

## 7. RewriterFunction (재작성자)

### 역할
기존 콘텐츠를 목적에 맞게 개선/재작성

### 입출력 구조
```csharp
public class RewriterInput
{
    public string Original { get; set; }
    public RewriteGoal Goal { get; set; } // Clarity | Brevity | Tone | Format
    public StyleGuide StyleGuide { get; set; }
    public List<string> PreserveElements { get; set; }
}

public class RewriterOutput
{
    public string Rewritten { get; set; }
    public List<Change> Changes { get; set; }
    public ImprovementMetrics Metrics { get; set; }
}
```

---

## 8. ExplainerFunction (설명자)

### 역할
복잡한 개념이나 프로세스를 이해하기 쉽게 설명

### 입출력 구조
```csharp
public class ExplainerInput
{
    public string Topic { get; set; }
    public string AudienceLevel { get; set; } // Beginner | Intermediate | Expert
    public ExplanationType Type { get; set; } // Concept | Process | Comparison
    public bool UseAnalogies { get; set; }
}

public class ExplainerOutput
{
    public string Explanation { get; set; }
    public List<Example> Examples { get; set; }
    public List<Analogy> Analogies { get; set; }
    public Glossary Terms { get; set; }
}
```

---

## 9. ReasonerFunction (추론자)

### 역할
논리적 추론과 의사결정 수행

### 입출력 구조
```csharp
public class ReasonerInput
{
    public List<Fact> Facts { get; set; }
    public List<Rule> Rules { get; set; }
    public string Question { get; set; }
    public ReasoningType Type { get; set; } // Deductive | Inductive | Abductive
}

public class ReasonerOutput
{
    public string Conclusion { get; set; }
    public List<ReasoningStep> Steps { get; set; }
    public ConfidenceLevel Confidence { get; set; }
    public List<Assumption> Assumptions { get; set; }
}
```

---

## 10. ConverterFunction (변환자)

### 역할
형식, 언어, 구조 간 변환

### 입출력 구조
```csharp
public class ConverterInput
{
    public string Source { get; set; }
    public Format SourceFormat { get; set; }
    public Format TargetFormat { get; set; }
    public ConversionOptions Options { get; set; }
}

public class ConverterOutput
{
    public string Converted { get; set; }
    public List<ConversionWarning> Warnings { get; set; }
    public LossMetrics DataLoss { get; set; }
}
```

---

## 11. VisualizerFunction (시각화자)

### 역할
데이터를 텍스트 기반 시각화로 표현

### 입출력 구조
```csharp
public class VisualizerInput
{
    public object Data { get; set; }
    public VisualizationType Type { get; set; } // Table | Chart | Graph | Diagram
    public string Format { get; set; } // Markdown | ASCII | Mermaid | PlantUML
}

public class VisualizerOutput
{
    public string Visualization { get; set; }
    public string Description { get; set; }
    public ViewOptions Options { get; set; }
}
```

---

## 12. DialogueManagerFunction (대화 관리자)

### 역할
대화 흐름 관리 및 컨텍스트 유지

### 입출력 구조
```csharp
public class DialogueInput
{
    public string UserMessage { get; set; }
    public ConversationHistory History { get; set; }
    public DialogueState State { get; set; }
}

public class DialogueOutput
{
    public string Response { get; set; }
    public DialogueState NewState { get; set; }
    public List<ClarificationNeeded> Clarifications { get; set; }
    public NextTurnExpectation Expectation { get; set; }
}
```

---

## 13. KnowledgeRetrieverFunction (지식 검색자)

### 역할
관련 정보 검색 및 구조화

### 입출력 구조
```csharp
public class RetrieverInput
{
    public string Query { get; set; }
    public List<Source> Sources { get; set; }
    public SearchStrategy Strategy { get; set; }
    public int MaxResults { get; set; }
}

public class RetrieverOutput
{
    public List<RetrievedInfo> Results { get; set; }
    public List<Source> Sources { get; set; }
    public RelevanceScores Scores { get; set; }
    public string Synthesis { get; set; }
}
```

---

## 14. MetaManagerFunction (메타 관리자)

### 역할
다른 Function들의 실행을 관리하고 최적화

### 입출력 구조
```csharp
public class MetaManagerInput
{
    public string Task { get; set; }
    public List<AvailableFunction> Functions { get; set; }
    public PerformanceHistory History { get; set; }
}

public class MetaManagerOutput
{
    public FunctionSelection Selection { get; set; }
    public ExecutionStrategy Strategy { get; set; }
    public QualityPrediction Prediction { get; set; }
    public List<Optimization> Optimizations { get; set; }
}
```

---

## 공통 설계 원칙

### 1. 에러 처리
```csharp
public abstract class BaseLLMFunction
{
    protected virtual async Task<ILLMResult> HandleError(Exception ex, ILLMContext context)
    {
        return ex switch
        {
            ParseException => await RetryWithClarification(context),
            TimeoutException => await UseCachedResponse(context),
            _ => new ErrorResult(ex.Message)
        };
    }
}
```

### 2. 캐싱 전략
```csharp
public interface IFunctionCache
{
    Task<T> GetOrComputeAsync<T>(string key, Func<Task<T>> factory);
}
```

### 3. 품질 보증
```csharp
public interface IQualityChecker
{
    Task<bool> MeetsQualityStandards(ILLMResult result);
    Task<ILLMResult> ImproveQuality(ILLMResult result);
}
```

### 4. 모니터링
```csharp
public interface IFunctionMetrics
{
    void RecordExecution(string function, TimeSpan duration, bool success);
    void RecordTokenUsage(string function, int tokens);
    void RecordQualityScore(string function, double score);
}
```