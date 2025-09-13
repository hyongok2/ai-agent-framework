# 개발 가이드라인 - 점진적 완성 방법론

## 🎯 핵심 철학

**"작은 단위의 완전한 구현"**
- 거대한 시스템을 한번에 구축하지 않는다
- 각 모듈을 완전히 독립적으로 개발하고 테스트한다
- 완성된 모듈만 다음 단계로 진행한다
- AI 도구와 개발자 모두 쉽게 파악할 수 있는 크기로 관리한다

## 📋 개발 단계별 계획

### Phase 1: LLM 모듈 완성 (독립적)
**목표**: LLM Functions를 완전히 독립적으로 구현하고 테스트 가능하게 구축

#### 1.1 LLM Provider 완성
- [x] ClaudeProvider 기본 구현
- [x] OpenAIProvider 기본 구현
- [ ] Provider별 독립 테스트 작성
- [ ] 설정 기반 Provider 선택 시스템
- [ ] Provider 성능/품질 검증

#### 1.2 LLM Functions 14가지 역할 구현
**dev_plan/llm.md 기준으로 단계별 구현**

**1차 핵심 Functions (4개)**
- [ ] **Planner**: 계획 수립 (오케스트레이션 핵심)
- [ ] **Generator**: 텍스트 생성 (기본 기능)
- [ ] **Analyzer**: 분석 및 해석 (정보 처리)
- [ ] **Summarizer**: 요약 (정보 압축)

**2차 지원 Functions (4개)**
- [ ] **Evaluator**: 품질 평가 및 피드백
- [ ] **Rewriter**: 개선 및 재작성
- [ ] **Explainer**: 설명 및 학습 지원
- [ ] **Tool Parameter Setter**: 도구 파라미터 구성

**3차 고급 Functions (6개)**
- [ ] **Reasoner**: 추론 및 논리적 판단
- [ ] **Converter**: 변환 (언어, 포맷, 코드)
- [ ] **Visualizer**: 텍스트 기반 시각화
- [ ] **Dialogue Manager**: 대화 흐름 관리
- [ ] **Knowledge Retriever**: 정보 조회
- [ ] **Meta-Manager**: 메타 분석 및 자기반성

#### 1.3 프롬프트 관리 시스템
- [ ] 파일 기반 프롬프트 템플릿
- [ ] 치환 시스템 (변수 주입)
- [ ] TTL 캐싱 전략
- [ ] 프롬프트별 독립 테스트

#### 1.4 LLM 독립 테스트
- [ ] 각 Function별 단위 테스트
- [ ] Provider별 성능 테스트
- [ ] 프롬프트 품질 검증
- [ ] 독립 실행 가능한 콘솔 앱

### Phase 2: Tools 모듈 완성 (독립적)
**목표**: Tools 시스템을 완전히 독립적으로 구현하고 테스트 가능하게 구축

#### 2.1 Tool Contract 시스템
- [ ] **Metadata**: Name, Description, Version, Category
- [ ] **Input Schema**: 입력 파라미터 정의 및 검증
- [ ] **Output Schema**: 출력 구조 정의 및 검증
- [ ] **Contract 검증**: 실행 시 계약 준수 확인

#### 2.2 Built-In Tools 구현
**dev_plan/tool.md 기준으로 필수 도구 구현**

**1차 필수 Tools (3개)**
- [ ] **WebSearchTool**: 웹 검색 (실제 API 연동)
- [ ] **FileSystemTool**: 파일 시스템 작업
- [ ] **CalculatorTool**: 계산 및 수식 처리

**2차 고급 Tools (3개)**
- [ ] **VectorDBTool**: RAG 검색 기능
- [ ] **DatabaseTool**: 데이터베이스 조회
- [ ] **EmbeddingTool**: 임베딩 및 유사도 계산

#### 2.3 Tools 독립 테스트
- [ ] 각 Tool별 단위 테스트
- [ ] Contract 검증 테스트
- [ ] 성능 및 안정성 테스트
- [ ] 독립 실행 가능한 콘솔 앱

### Phase 3: 모듈 통합 및 Orchestration
**목표**: 완성된 LLM과 Tools 모듈을 통합하고 오케스트레이션 구현

#### 3.1 Registry 시스템 (단순)
- [ ] LLM Functions 등록 및 관리
- [ ] Tools 등록 및 관리
- [ ] 동적 발견 및 로딩
- [ ] 메타데이터 기반 검색

#### 3.2 오케스트레이션 엔진
**dev_plan/orchestration.md 기준 Plan-Execute 구조**
- [ ] **Plan LLM**: 계획 수립 핵심 로직
- [ ] **Execution Engine**: LLM Functions + Tools 실행
- [ ] **Flow Control**: 다음 단계 결정 로직
- [ ] **Completion Detection**: 완료 조건 판단

#### 3.3 통합 테스트
- [ ] End-to-End 워크플로우 테스트
- [ ] 성능 및 안정성 검증
- [ ] 사용자 시나리오 테스트

### Phase 4: 확장성 및 사용자 인터페이스
**목표**: 완성된 시스템의 확장성과 사용성 개선

#### 4.1 확장 시스템
- [ ] Plug-In Tools 지원
- [ ] MCP Tools 지원
- [ ] 설정 기반 모델 전환
- [ ] 프롬프트 외부 관리

#### 4.2 사용자 인터페이스
- [ ] Console 애플리케이션
- [ ] Web API
- [ ] 관리 도구

## 🎯 품질 기준

### 각 모듈 완성 조건
1. **독립 실행 가능**: 다른 모듈 없이 단독 실행 및 테스트 가능
2. **완전한 테스트**: 단위 테스트 + 통합 테스트 + 성능 테스트
3. **명확한 인터페이스**: 다른 모듈과의 연동 인터페이스 명확 정의
4. **문서화 완료**: 사용법, API, 예제 문서 작성
5. **검증 완료**: 실제 사용 시나리오로 검증

### 진행 원칙
- **완성도 우선**: 다음 단계 진행 전 현재 단계 100% 완성
- **독립성 유지**: 각 모듈은 완전히 독립적으로 동작
- **점진적 통합**: 완성된 모듈만 통합 과정에 포함
- **지속적 검증**: 각 단계마다 실제 동작 확인

## 📊 현재 상태 (2025-01-01)

### ✅ 완료된 작업
- [x] 프로젝트 구조 단순화 (Core + LLM + Tools)
- [x] 불필요한 의존성 제거 (Registry 자동등록 등)
- [x] 기본 LLM Provider 구현 (Claude, OpenAI)
- [x] 기본 Function/Tool 구조 정의

### 🔄 진행 중인 작업
- [ ] LLM Functions 14가지 역할 구현 시작
- [ ] Tools Contract 시스템 구축

### 📝 다음 우선순위
1. **LLM Generator Function 완성** - 가장 기본적인 텍스트 생성 기능
2. **독립 테스트 작성** - Generator Function 단독 실행 및 검증
3. **프롬프트 템플릿 시스템** - 파일 기반 프롬프트 관리
4. **LLM Planner Function 구현** - 오케스트레이션 핵심 기능

## 🚀 성공 지표

### Phase 1 완료 기준
- [ ] 14개 LLM Functions 모두 독립 실행 가능
- [ ] 각 Function별 성능 및 품질 검증 완료
- [ ] 프롬프트 관리 시스템 완전 구축
- [ ] 독립 콘솔 앱으로 모든 Function 테스트 가능

### Phase 2 완료 기준
- [ ] 6개 핵심 Tools 모두 독립 실행 가능
- [ ] Contract 시스템으로 모든 Tool 검증 가능
- [ ] 각 Tool별 성능 및 안정성 검증 완료
- [ ] 독립 콘솔 앱으로 모든 Tool 테스트 가능

### Phase 3 완료 기준
- [ ] Plan-Execute 오케스트레이션 완전 구현
- [ ] 실제 사용자 시나리오 End-to-End 동작
- [ ] 성능 및 안정성 기준 모두 달성

이 방식으로 **각 단계에서 완전한 성과물을 확보하면서 점진적으로 발전**시켜나갑니다.