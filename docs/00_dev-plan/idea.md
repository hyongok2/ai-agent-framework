# 🧭 AI Agent Framework 개요 문서

---

## 1. 기조 (Principles)

1. **추상화 중심 (Abstraction First)**

   * Agent의 핵심 기능을 **오케스트레이션 / 도구 / LLM 호출**이라는 3계층 추상화로 단순화
   * 모든 구현체는 공통 인터페이스를 준수 → 교체/확장 용이

2. **표준화 & 스키마 기반 (Schema-First)**

   * 모든 입력/출력은 **JSON Schema**로 정의 및 검증
   * 일관된 데이터 구조 보장, LLM 응답의 신뢰성 강화

3. **스트리밍 우선 (Streaming-First)**

   * LLM/도구/오케스트레이션 모든 결과는 **Chunk 단위 스트리밍**으로 전달
   * 실시간 UX 확보, 긴 처리 과정에서도 중간 피드백 제공

4. **환경 독립성 (Cloud + Local)**

   * 클라우드 LLM과 로컬 LLM 모두 지원하는 **범용 프레임워크**
   * 실행 정책(한 번에 처리 vs 잘게 쪼개기)을 Capabilities 기반으로 선택

5. **Config & Plugin 친화적**

   * 도구/LLM 설정은 별도 파일 관리 → 환경별 분리 가능 (dev/prod/local)
   * 플러그인·MCP 도구 쉽게 연결 → 생태계 확장성 확보

---

## 2. 핵심 개념 (Core Concepts)

### (1) 오케스트레이션 (Orchestration)

* 실행 전략을 정의하는 최상위 추상화
* 유형:

  * **Simple**: 단순 응답
  * **Fixed**: 사전 정의된 Workflow 실행
  * **Planner**: LLM이 플랜(JSON) 생성 후 실행
  * **Reactive**: 실행 중간에도 재계획 가능
* 모든 실행은 **Plan → Step → StreamChunk** 구조로 진행

---

### (2) 도구 (Tools)

* Agent의 **실행 능력**을 확장하는 구성 요소
* 유형:

  * **내부 도구** (기본 제공)
  * **플러그인 도구** (동적 로딩)
  * **MCP 도구** (외부 시스템 연동)
* 도구는 `ITool` 인터페이스와 `ToolDescriptor` 스키마를 준수
* 각 도구별 Config + Secret 관리 지원

---

### (3) LLM (Large Language Model)

* 다양한 모델을 동일한 인터페이스(`ILlmClient`)로 호출
* **CallType** 기반으로 역할 세분화:

  * Chat, JSON 응답, Plan, ToolCallSuggest, VisionQA, Embed 등
* **Model Profiles**로 실행 전략 추상화:

  * fast / analytic / local / secure 등
* Prompt Engine + Schema Validator로 **안정적인 출력** 보장

---

## 3. 단계별 구축 전략

### **1단계. 추상화 정의**

* RunId, StepId, StreamChunk, Step, Plan 등 Core Contracts 확립
* `ITool`, `ILlmClient`, `ISchemaValidator` 인터페이스 정의

### **2단계. 도구 계층**

* Tool Registry, Config Provider, Secret Resolver 구현
* Built-in/Plugin/MCP 도구 통합 로딩 가능

### **3단계. LLM 계층**

* CallType & Capabilities 확정
* Provider Adapter(OpenAI, Claude, Ollama 등) 구현
* Prompt Engine & Model Profiles 관리

### **4단계. 오케스트레이션 계층**

* OrchestrationType Selector (Rule-based + LLM 기반)
* Executor 4종(Simple, Fixed, Planner, Reactive)
* StepRunner + Streaming Aggregator 구현

### **5단계. 실행/운영 레이어**

* Gateway(CLI, HTTP/gRPC)
* Config Snapshot 저장 → 재현성 확보
* Observability(로그, 메트릭, 트레이스)
* Eval Harness(자동 평가 툴킷)
* 환경별 실행 모드(Local/Cloud/On-Prem)

---

## 4. 확장 시나리오

1. **멀티에이전트 협업**

   * Executor 확장: MultiAgentExecutor
   * Plan schema 확장: 에이전트 간 메시지 교환

2. **보안/정책 레이어 추가**

   * Guardrails (금칙어, PII 마스킹)
   * RBAC (도구 사용 권한 제어)
   * Budget Control (비용/토큰 제한)

3. **멀티모달 확장**

   * Vision, AudioIn/Out, Video CallType 추가

---

## 5. 컨셉 요약

* **AI Agent Framework = 오케스트레이션 + 도구 + LLM**
* **Schema-first, Streaming-first, Config-first**라는 3대 기조
* **클라우드와 로컬을 아우르는 범용 아키텍처**
* 확장성과 운영성을 내장한 **표준 프레임워크 기반**

---
