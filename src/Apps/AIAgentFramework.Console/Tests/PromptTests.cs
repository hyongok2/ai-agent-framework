using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.LLM.Services.Summarization;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Console.Tests;

public static class PromptTests
{
    public static async Task TestPromptRegistry(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - PromptRegistry 테스트 ===\n");

        System.Console.WriteLine($"등록된 프롬프트 수: {promptRegistry.GetAllPrompts().Count}");
        foreach (var promptDef in promptRegistry.GetAllPrompts())
        {
            System.Console.WriteLine($"  - {promptDef.Name} ({promptDef.Role}): {promptDef.Metadata.Description}");
        }
        System.Console.WriteLine();

        var summarizePrompt = promptRegistry.GetPrompt("summarization");
        System.Console.WriteLine($"프롬프트: {summarizePrompt?.Template.Substring(0, 100)}...");
        System.Console.WriteLine($"변수: {string.Join(", ", summarizePrompt?.Variables ?? new List<string>())}\n");

        var variables = new Dictionary<string, object>
        {
            ["CURRENT_TIME"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            ["CONTENT"] = GetCompextText(),
            ["SUMMARY_STYLE"] = SummaryStyle.Brief,
            ["REQUIREMENTS"] = "핵심 내용 위주로 요약"
        };

        var validation = promptRegistry.ValidateVariables("summarization", variables);
        System.Console.WriteLine($"변수 검증: {(validation.IsValid ? "성공" : validation.ErrorMessage)}\n");

        var renderedPrompt = summarizePrompt!.Render(variables);

        System.Console.WriteLine("--- summarization 테스트 ---");
        var response = await ollama.CallAsync(renderedPrompt, "gpt-oss:20b");
        System.Console.WriteLine($"LLM 응답:\n{response}\n");

        System.Console.WriteLine("=== PromptRegistry 테스트 완료 ===");
    }

    private static string GetCompextText()
    {
        return @"
AI Agent Framework는 대규모 언어 모델(LLM)을 활용한 자율 에이전트 시스템을 구축하기 위한 종합 프레임워크입니다.

## 핵심 기능

1. **LLM 통합**: Ollama, OpenAI, Anthropic 등 다양한 LLM 제공자를 통합 인터페이스로 지원합니다.
   - 스트리밍 모드: 실시간 토큰 생성을 통해 사용자 경험을 개선합니다.
   - 비동기 처리: async/await 패턴으로 고성능 병렬 처리를 구현했습니다.
   - 재시도 로직: 네트워크 장애나 일시적 오류에 대한 자동 재시도 메커니즘을 제공합니다.

2. **Tool 시스템**: 에이전트가 외부 시스템과 상호작용할 수 있는 확장 가능한 도구 인터페이스를 제공합니다.
   - FileReader: 로컬 파일 시스템에서 텍스트, JSON, XML 등 다양한 포맷을 읽습니다.
   - FileWriter: UTF-8 인코딩으로 파일을 생성하고 수정합니다.
   - Calculator: 수학 표현식을 안전하게 평가하고 계산 결과를 반환합니다.
   - WebSearchTool (예정): 검색 엔진 API를 통한 웹 검색 기능
   - DatabaseTool (예정): SQL 쿼리 실행 및 결과 조회

3. **Prompt 관리**: YAML 기반 프롬프트 템플릿 시스템으로 재사용 가능한 프롬프트를 관리합니다.
   - 변수 치환: Mustache 스타일 {{VARIABLE}} 문법 지원
   - 메타데이터: 각 프롬프트의 역할, 설명, 버전 정보 포함
   - 검증: 필수 변수 누락 시 명확한 오류 메시지 제공

4. **실행 계획**: Task Planner가 사용자 요청을 분석하여 단계별 실행 계획을 자동 생성합니다.
   - 의존성 관리: DAG(Directed Acyclic Graph) 기반 단계 의존성 추적
   - 병렬 실행: 독립적인 단계는 동시 실행하여 성능 최적화
   - 오류 처리: 특정 단계 실패 시 롤백 또는 대체 경로 선택

5. **평가 시스템**: Evaluator가 실행 결과의 품질과 정확성을 자동으로 평가합니다.
   - 점수화: 0-100점 스케일로 결과 품질 측정
   - 개선 제안: 부족한 부분에 대한 구체적인 개선 방향 제시
   - 기준 검증: 사용자 정의 평가 기준에 따른 자동 검증

## 아키텍처 원칙

- **SOLID 원칙 준수**: 각 컴포넌트는 단일 책임을 가지며 확장에는 열려있고 수정에는 닫혀있습니다.
- **의존성 주입**: 생성자 주입 패턴으로 테스트 용이성과 유연성을 확보했습니다.
- **계층 분리**: Core, LLM, Tools, Execution 레이어로 명확히 분리하여 관심사를 격리했습니다.
- **인터페이스 우선**: 구현이 아닌 추상화에 의존하여 느슨한 결합을 유지합니다.

## 사용 예시

```csharp
// 1. Provider 초기화
var ollama = new OllamaProvider(""http://localhost:11434"");

// 2. Registry 설정
var promptRegistry = new PromptRegistry();
promptRegistry.LoadFromDirectory(""./prompts"");

// 3. LLM Function 생성
var planner = new TaskPlannerFunction(promptRegistry, ollama, toolRegistry, llmRegistry);

// 4. 계획 생성
var input = new PlanningInput
{
    UserRequest = ""파일을 읽고 내용을 요약해서 새 파일에 저장해줘""
};
var result = await planner.ExecuteAsync(input);

// 5. 계획 실행
var executor = new PlanExecutor(toolRegistry, llmRegistry);
var executionResult = await executor.ExecuteAsync(result.ParsedData as PlanningResult);
```

## 성능 특성

- **메모리 사용**: 평균 50-100MB (프롬프트 캐싱 포함)
- **응답 시간**: 단순 쿼리 200-500ms, 복잡한 계획 1-3초 (LLM 속도 의존적)
- **동시성**: 최대 10개 병렬 Tool 실행 지원
- **확장성**: 수평 확장 가능한 무상태(stateless) 설계

## 향후 계획

1. **메모리 시스템**: 대화 히스토리와 컨텍스트를 장기 저장하는 메모리 레이어 추가
2. **에이전트 오케스트레이션**: 여러 전문 에이전트 간 협업 및 작업 위임 메커니즘
3. **관찰성(Observability)**: 분산 추적, 메트릭 수집, 로깅 통합
4. **보안 강화**: Tool 실행 샌드박싱, 입력 검증, Rate Limiting
5. **UI/UX 개선**: 웹 기반 대시보드와 실시간 모니터링 도구

이 프레임워크는 C# 개발자가 손쉽게 AI 기반 자동화 시스템을 구축할 수 있도록 설계되었으며,
프로덕션 환경에서의 안정성과 확장성을 최우선으로 고려하였습니다.
";
    }
}
