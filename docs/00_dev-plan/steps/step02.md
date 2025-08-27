# Stage 2: LLM 추상화 계층

## 개요
LLM 호출을 추상화하고 기본적인 프롬프트 관리 시스템을 구축하는 단계

## 목표
- LLM Provider 추상화 계층 구현
- OpenAI GPT 초기 통합
- 프롬프트 관리 시스템 구축
- 응답 파싱 메커니즘 확립

## 의존성
- Stage 1: 기초 인프라 (인터페이스 및 Base Class 활용)

## LLM Provider 시스템

### 1. Provider 추상화
```
구조:
ILLMProvider (인터페이스)
├── BaseLLMProvider (추상 클래스)
│   ├── OpenAIProvider
│   ├── ClaudeProvider (향후)
│   └── LocalLLMProvider (향후)
```

### 2. OpenAI Provider 구현
- GPT-4 모델 연동
- API 키 관리 및 보안
- Rate Limiting 처리
- 재시도 로직 구현
- 토큰 사용량 추적

### 3. Provider 설정
- 모델 선택 (gpt-4, gpt-3.5-turbo 등)
- Temperature, MaxTokens 등 파라미터
- Timeout 설정
- 에러 처리 정책

## 프롬프트 관리 시스템

### 1. 프롬프트 구조
```
prompts/
├── system/
│   └── base_system.md
├── functions/
│   ├── planner.md
│   ├── analyzer.md
│   ├── generator.md
│   └── [각 LLM 기능별 프롬프트]
└── templates/
    └── common_templates.md
```

### 2. 프롬프트 로더
- 파일 시스템 기반 로더
- 프롬프트 캐싱 메커니즘
- Hot reload 지원
- 프롬프트 버전 관리

### 3. 치환 시스템
- 변수 정의 및 치환 규칙
- 플레이스홀더 형식: `{{variable_name}}`
- 조건부 섹션 지원
- 반복 섹션 지원
- 중첩 치환 지원

### 4. 프롬프트 검증
- 필수 변수 확인
- 구문 검증
- 길이 제한 확인
- 토큰 수 계산

## LLM Function 구현 (Base Class 활용)

### 1. PlannerFunction (첫 번째 구현)
```
상속 구조:
BaseLLMFunction
└── PlannerFunction
    - Role: "Planner"
    - PromptTemplate: "planner.md"
    - 특화 메서드: CreatePlan(), ValidatePlan()
```

### 2. 공통 기능 구현 (BaseLLMFunction)
- 프롬프트 로드 및 치환
- Provider 호출
- 응답 수신 및 기본 검증
- 에러 처리 및 재시도
- 실행 메트릭 수집

### 3. Function 등록 시스템
- Function Registry 구현
- 자동 발견 메커니즘
- Function 메타데이터 관리
- Function 생명주기 관리

## JSON 응답 처리

### 1. 응답 구조 정의
```
기본 구조:
{
  "function": "string",
  "status": "success|partial|error",
  "result": { ... },
  "next_step": { ... },
  "metadata": { ... }
}
```

### 2. 파싱 시스템
- JSON Schema 정의
- 스키마 기반 검증
- 유연한 파싱 (부분 성공 허용)
- 파싱 실패 시 복구 전략

### 3. 타입 안전성
- 강타입 DTO 정의
- 자동 역직렬화
- 타입 변환 처리
- Null 처리 정책

## 실행 컨텍스트 관리

### 1. LLM 실행 컨텍스트
- 대화 이력 관리
- 시스템 메시지 관리
- 토큰 예산 관리
- 실행 메타데이터

### 2. 컨텍스트 전달
- Function 간 컨텍스트 공유
- 컨텍스트 격리 수준
- 컨텍스트 압축 전략
- 컨텍스트 만료 정책

## 테스트 전략

### 1. Provider 테스트
- Mock Provider 구현
- API 호출 시뮬레이션
- 에러 상황 테스트
- 성능 테스트

### 2. 프롬프트 테스트
- 치환 정확성 테스트
- 프롬프트 로드 테스트
- 캐싱 동작 테스트
- 변수 누락 테스트

### 3. Function 테스트
- PlannerFunction 단위 테스트
- 응답 파싱 테스트
- 에러 처리 테스트
- 통합 테스트

## 검증 기준

### 필수 검증 항목
- [ ] OpenAI API 연결 및 응답 수신 성공
- [ ] 프롬프트 파일 로드 및 치환 정상 동작
- [ ] PlannerFunction이 계획 생성 가능
- [ ] JSON 응답 파싱 성공률 95% 이상
- [ ] 에러 발생 시 적절한 복구 동작
- [ ] 토큰 사용량 정확히 추적
- [ ] BaseLLMFunction 상속 구조 정상 동작

### 성능 기준
- API 호출 지연시간 < 5초
- 프롬프트 캐시 적중률 > 80%
- 메모리 사용량 < 100MB
- 동시 요청 처리 가능

## 산출물

### 1. 구현 코드
- OpenAIProvider 완전 구현
- PlannerFunction 구현
- 프롬프트 로더 시스템
- JSON 파서 구현

### 2. 프롬프트 템플릿
- 기본 시스템 프롬프트
- Planner 프롬프트
- 공통 템플릿 조각
- 프롬프트 작성 가이드

### 3. 설정 파일
- Provider 설정 스키마
- 모델별 최적 파라미터
- 프롬프트 설정
- 캐싱 정책 설정

### 4. 테스트
- Provider 테스트 슈트
- Function 테스트 슈트
- 통합 테스트 시나리오
- 부하 테스트 스크립트

## 위험 요소 및 대응

### 1. API 비용 관리
- **위험**: 개발 중 과도한 API 호출 비용
- **대응**: Mock Provider 우선 사용, 호출 제한 설정

### 2. 응답 일관성
- **위험**: LLM 응답이 예상 형식을 벗어남
- **대응**: 프롬프트 엔지니어링, 검증 계층 강화

### 3. Provider 종속성
- **위험**: OpenAI 서비스 장애 시 전체 시스템 마비
- **대응**: Fallback Provider 준비, 캐싱 적극 활용

### 4. BaseLLMFunction 설계 부족
- **위험**: 추가 Function 구현 시 Base Class 수정 필요
- **대응**: Template Method 패턴 활용, 확장 포인트 충분히 제공

## 예상 소요 시간
- Provider 추상화: 1일
- OpenAI Provider: 1일
- 프롬프트 시스템: 1.5일
- PlannerFunction: 1일
- JSON 파싱: 0.5일
- 테스트 작성: 1일
- **총 예상: 6일**

## 다음 단계 준비
- Tool 시스템과의 연동 인터페이스 확정
- 추가 LLM Function 구현 준비
- 오케스트레이션과의 통합 포인트 정의