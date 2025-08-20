# Step 1: Core Contracts - Overview

## 목표
AI Agent Framework의 핵심 추상화 계층을 정의하고 구현

## 주요 구성요소
1. **기본 식별자** (RunId, StepId)
2. **스트리밍 이벤트** (StreamChunk 계층)
3. **실행 단위** (Step, Plan)
4. **도구 추상화** (ITool, ToolDescriptor)
5. **LLM 추상화** (ILlmClient, LlmRequest)
6. **스키마 검증** (ISchemaValidator)

## 실행 순서
- Phase 1: 프로젝트 초기 설정
- Phase 2: 핵심 식별자 구현
- Phase 3: 스트리밍 이벤트 구조
- Phase 4: Step & Plan 구조
- Phase 5: 도구 추상화
- Phase 6: LLM 추상화
- Phase 7: JSON Schema 검증
- Phase 8: 통합 테스트

## 예상 기간
2-3주 (15일)

## 성공 기준
- [ ] 모든 Core Contract 타입 정의
- [ ] 단위 테스트 커버리지 80% 이상
- [ ] JSON Schema 5개 이상 정의
- [ ] 완전한 문서화