# AI 에이전트 프레임워크 구현 작업 목록

## 1. 프로젝트 구조 및 핵심 인터페이스 설정

- [x] 1.1 프로젝트 구조 생성 및 기본 설정

  - .NET 8 솔루션 및 프로젝트 생성 (AIAgentFramework.sln)
  - 핵심 프로젝트 구조 생성: Core, LLM, Tools, Registry, Configuration, Tests
  - NuGet 패키지 참조 추가: Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Configuration, Microsoft.Extensions.Caching.Memory, Newtonsoft.Json
  - _요구사항: 1.1, 8.1_

- [x] 1.2 핵심 인터페이스 정의
  - IOrchestrationEngine, IOrchestrationContext, IExecutionStep 인터페이스 작성
  - ILLMFunction, ILLMContext, ILLMResult 인터페이스 작성
  - ITool, IToolContract, IToolInput, IToolResult 인터페이스 작성
  - IRegistry, IPromptManager, ILLMProvider 인터페이스 작성
  - _요구사항: 1.1, 2.1, 3.1_

## 2. 설정 관리 시스템 구현

- [x] 2.1 설정 모델 클래스 구현

  - LLMConfiguration, ToolConfiguration, PromptConfiguration 클래스 작성
  - ConfigurationManager 클래스 구현 (YAML/JSON 설정 로드, 런타임 재로드 지원)
  - 환경별 설정 분리 로직 구현 (Development, Testing, Production)
  - _요구사항: 10.1, 10.2, 10.3_

- [x] 2.2 설정 파일 템플릿 생성
  - config.yaml 기본 템플릿 작성 (LLM 모델 매핑, 도구 설정, UI 설정 포함)
  - 환경별 설정 파일 생성 (config.development.yaml, config.production.yaml)
  - 설정 검증 로직 구현 (필수 설정 항목 확인, 타입 검증)
  - _요구사항: 10.1, 10.5, 10.6_

## 3. Registry 시스템 구현

- [x] 3.1 기본 Registry 클래스 구현

  - Registry 클래스 작성 (ILLMFunction, ITool 등록/조회 기능)
  - 메타 프로그래밍 기반 자동 등록 메커니즘 구현 (Reflection, Attribute 활용)
  - 중앙 집중식 메타데이터 관리 기능 구현
  - _요구사항: 8.1, 8.2, 8.5, 8.6_

- [x] 3.2 Attribute 기반 등록 시스템 구현
  - LLMFunctionAttribute, BuiltInToolAttribute, PluginToolAttribute 클래스 작성
  - AutoRegisterFromAssembly 메서드 구현 (Assembly 스캔 및 자동 등록)
  - 동적 발견 및 등록 메커니즘 구현 (런타임 타입 정보 활용)
  - _요구사항: 8.1, 8.2, 8.5_

## 4. LLM 시스템 핵심 구현

- [x] 4.1 LLM Provider 추상화 계층 구현

  - ILLMProvider 인터페이스 구현체 작성: OpenAIProvider, ClaudeProvider, LocalLLMProvider
  - LLMProviderFactory 클래스 구현 (설정 기반 모델 선택)
  - 각 Provider별 API 통신 로직 구현 (HTTP 클라이언트, 인증, 오류 처리)
  - _요구사항: 2.5, 7.2_

- [x] 4.2 프롬프트 관리 시스템 구현

  - PromptManager 클래스 구현 (파일 기반 프롬프트 로드, 치환 시스템)
  - TTL 캐싱 전략 구현 (IMemoryCache 활용)
  - 프롬프트 템플릿 치환 로직 구현 (변수명 통일, 동적 요소 주입)
  - _요구사항: 2.2, 2.3, 10.4_

- [x] 4.3 14가지 LLM 기능 기본 구현

  - PlannerFunction 클래스 구현 (사용자 요구 분석, 실행 계획 수립)
  - InterpreterFunction, SummarizerFunction, GeneratorFunction 클래스 구현
  - EvaluatorFunction, RewriterFunction, ExplainerFunction, ReasonerFunction 클래스 구현
  - ConverterFunction, VisualizerFunction, ToolParameterSetterFunction 클래스 구현
  - DialogueManagerFunction, KnowledgeRetrieverFunction, MetaManagerFunction 클래스 구현
  - _요구사항: 2.1, 2.4_

- [x] 4.4 LLM 응답 파싱 시스템 구현
  - JSON 응답 파싱 로직 구현 (구조화된 응답 처리)
  - 예외 응답 처리 로직 구현 (사용자 응답, 특수 기능)
  - 응답 타입별 파싱 클래스 구현 (공통 요소 상속, 개별 클래스 구현)
  - _요구사항: 2.4_

## 5. 도구 시스템 구현

- [x] 5.1 Built-In Tools 구현

  - EmbeddingCacheTool 클래스 구현 (임베딩 벡터 캐싱 및 검색)
  - VectorDBTool 클래스 구현 (벡터 데이터베이스 검색, RAG 기능)
  - 각 Built-In Tool의 Contract 검증 로직 구현 (입출력 스키마 검증)
  - _요구사항: 4.1, 4.3_

- [x] 5.2 Plug-In Tools 시스템 구현

  - PluginLoader 클래스 구현 (DLL 파일 동적 로딩, Reflection 기반 타입 스캔)
  - IPluginTool 인터페이스 구현 (버전, 작성자, 의존성 정보 포함)
  - Manifest 파일 기반 플러그인 메타데이터 관리 구현
  - 독립적인 플러그인 설정 관리 시스템 구현
  - _요구사항: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 5.3 MCP Tools 시스템 구현

  - IMCPTool 인터페이스 및 MCPToolAdapter 클래스 구현
  - MCP 클라이언트 구현 (JSON-RPC 2.0 기반 통신)
  - MCP 프로토콜 표준 준수 검증 로직 구현
  - 다양한 전송 방식 지원 (HTTP, WebSocket, stdio)
  - _요구사항: 6.1, 6.2, 6.3, 6.4_

- [ ] 5.4 도구 실행 및 파라미터 처리 시스템 구현
  - 파라미터 없는 도구 직접 호출 로직 구현
  - LLM Tool Parameter Setter를 통한 파라미터 구성 로직 구현
  - 도구 실행 결과 표준화 처리 구현 (IToolResult 통일)
  - _요구사항: 3.5, 3.6_

## 6. 오케스트레이션 엔진 구현

- [ ] 6.1 핵심 오케스트레이션 로직 구현

  - OrchestrationEngine 클래스 구현 ([계획-실행] 흐름 관리)
  - OrchestrationContext 클래스 구현 (세션 관리, 실행 이력, 공유 데이터)
  - ExecutionStep 클래스 구현 (단계별 실행 정보 추적)
  - _요구사항: 1.1, 1.2, 1.3_

- [ ] 6.2 실행 흐름 제어 로직 구현

  - LLM Plan 기능 호출 및 결과 처리 로직 구현
  - 계획된 기능 순차 실행 로직 구현 (LLM 기능, Tool 기능 분기)
  - 완료 조건 확인 및 오케스트레이션 종료 로직 구현
  - _요구사항: 1.4, 1.5, 1.6_

- [ ] 6.3 컨텍스트 관리 및 상태 추적 구현
  - 세션별 컨텍스트 생성 및 관리 로직 구현
  - 실행 단계별 상태 추적 및 이력 관리 구현
  - 공유 데이터 관리 및 단계 간 데이터 전달 구현
  - _요구사항: 1.1, 1.2_

## 7. 오류 처리 및 품질 관리 시스템 구현

- [ ] 7.1 계층별 오류 처리 구현

  - ErrorHandlingMiddleware 클래스 구현 (LLM, Tool, 시스템 오류 분류 처리)
  - 사용자 정의 예외 클래스 구현 (LLMException, ToolException)
  - 오류 로깅 및 모니터링 시스템 구현
  - _요구사항: 9.2, 9.3_

- [ ] 7.2 품질 관리 및 검증 시스템 구현
  - Contract 검증 로직 구현 (JSON Schema 기반 입출력 검증)
  - FaultTolerantExecutor 클래스 구현 (오류 격리, 대체 실행)
  - 성능 모니터링 및 메트릭 수집 시스템 구현
  - _요구사항: 9.1, 9.4, 9.5, 9.6_

## 8. 사용자 인터페이스 계층 구현

- [ ] 8.1 Web Interface 구현

  - ASP.NET Core Web API 프로젝트 생성
  - RESTful API 엔드포인트 구현 (/api/orchestration/execute, /api/orchestration/continue)
  - 웹 기반 사용자 인터페이스 구현 (HTML, JavaScript, CSS)
  - _요구사항: 7.4_

- [ ] 8.2 Console Interface 구현

  - 콘솔 애플리케이션 프로젝트 생성
  - 명령줄 인터페이스 구현 (명령어 파싱, 대화형 모드)
  - 배치 처리 모드 구현 (파일 입력, 결과 출력)
  - _요구사항: 7.4_

- [ ] 8.3 API Interface 구현
  - OpenAPI/Swagger 문서 생성
  - API 인증 및 권한 관리 구현
  - API 버전 관리 및 하위 호환성 구현
  - _요구사항: 7.4_

## 9. 프롬프트 템플릿 및 설정 파일 작성

- [ ] 9.1 14가지 LLM 기능별 프롬프트 템플릿 작성

  - prompts/planner.md 작성 (계획 수립 프롬프트, JSON 응답 구조 정의)
  - prompts/interpreter.md, prompts/summarizer.md, prompts/generator.md 작성
  - prompts/evaluator.md, prompts/rewriter.md, prompts/explainer.md 작성
  - prompts/reasoner.md, prompts/converter.md, prompts/visualizer.md 작성
  - prompts/tool_parameter_setter.md, prompts/dialogue_manager.md 작성
  - prompts/knowledge_retriever.md, prompts/meta_manager.md 작성
  - _요구사항: 2.2, 2.3_

- [ ] 9.2 프롬프트 치환 요소 및 응답 구조 정의
  - 각 프롬프트별 치환 변수 정의 ({user_request}, {context}, {tools_list} 등)
  - JSON 응답 구조 표준화 (success, data, next_actions, is_completed 필드)
  - 예외 응답 처리 규칙 정의 (사용자 응답, 특수 기능 응답)
  - _요구사항: 2.3, 2.4_

## 10. 테스트 구현

- [ ] 10.1 단위 테스트 구현

  - LLM 기능별 단위 테스트 작성 (각 14가지 기능의 독립적 테스트)
  - Built-In Tools 단위 테스트 작성 (입출력 검증, Contract 검증)
  - 프롬프트 템플릿 치환 테스트 작성
  - Registry 시스템 단위 테스트 작성 (등록, 조회, 자동 발견)
  - _요구사항: 모든 요구사항의 개별 기능 검증_

- [ ] 10.2 통합 테스트 구현

  - 오케스트레이션 엔진 전체 흐름 테스트 작성
  - LLM과 도구 간 상호작용 테스트 작성
  - 플러그인 로딩 및 실행 통합 테스트 작성
  - MCP 도구 연결 및 실행 통합 테스트 작성
  - _요구사항: 1.1~1.6, 2.1~2.5, 3.1~3.6_

- [ ] 10.3 확장성 및 성능 테스트 구현
  - 플러그인 동적 로딩 테스트 작성
  - 대량 요청 처리 성능 테스트 작성
  - 메모리 사용량 및 캐싱 효율성 테스트 작성
  - 오류 처리 및 복구 테스트 작성
  - _요구사항: 7.1~7.5, 8.1~8.6, 9.1~9.6_

## 11. 샘플 플러그인 및 데모 구현

- [ ] 11.1 샘플 플러그인 도구 구현

  - 웹 검색 플러그인 구현 (HTTP 요청, HTML 파싱)
  - 파일 처리 플러그인 구현 (텍스트 파일 읽기/쓰기, CSV 처리)
  - 데이터베이스 연결 플러그인 구현 (SQL 쿼리 실행)
  - _요구사항: 5.1~5.5_

- [ ] 11.2 도메인 특화 에이전트 데모 구현
  - 개발 에이전트 데모 (코드 분석 도구 + 개발 특화 프롬프트)
  - 문서 처리 에이전트 데모 (문서 분석 도구 + 요약 특화 프롬프트)
  - 데이터 분석 에이전트 데모 (데이터 처리 도구 + 분석 특화 프롬프트)
  - _요구사항: 7.1~7.3_

## 12. 문서화 및 배포 준비

- [ ] 12.1 개발자 문서 작성

  - API 문서 작성 (OpenAPI/Swagger 기반)
  - 플러그인 개발 가이드 작성 (인터페이스 구현, 배포 방법)
  - 설정 가이드 작성 (LLM 모델 설정, 도구 설정, 프롬프트 커스터마이징)
  - _요구사항: 모든 요구사항의 사용법 문서화_

- [ ] 12.2 배포 패키지 준비
  - NuGet 패키지 생성 (Core, LLM, Tools 라이브러리)
  - Docker 이미지 생성 (Web Interface, Console Interface)
  - 설치 스크립트 작성 (의존성 설치, 초기 설정)
  - _요구사항: 시스템 배포 및 운영 지원_
