# AI Agent Framework

확장 가능한 [계획-실행] 구조의 오케스트레이션을 통해 다양한 도메인 특화 에이전트를 개발할 수 있는 플랫폼입니다.

## 프로젝트 구조

```
AIAgentFramework/
├── AIAgentFramework.Core/              # 핵심 인터페이스 및 모델
│   ├── Interfaces/                     # 핵심 인터페이스
│   ├── Models/                         # 기본 모델 클래스
│   └── Exceptions/                     # 예외 클래스
├── AIAgentFramework.LLM/               # LLM 시스템 구현
├── AIAgentFramework.Tools/             # 도구 시스템 구현
├── AIAgentFramework.Registry/          # 레지스트리 시스템
├── AIAgentFramework.Configuration/     # 설정 관리
└── AIAgentFramework.Tests/            # 테스트
```

## 핵심 설계 철학

- **고정된 오케스트레이션**: 전체 흐름은 고정적으로 사용
- **5가지 튜닝 요소**: 도구, LLM 모델, 프롬프트, UI, LLM 기능을 통한 확장
- **우아한 확장**: if-else 조건 분기를 지양하고 우아한 확장 구조 추구
- **타입 투명성**: 외부에서 도구 호출 시 타입(내장/플러그인/MCP) 알 필요 없음

## 시스템 아키텍처

```
사용자 입력 → LLM Plan → 기능 실행 → LLM Plan → ... → 완료
```

## 빌드 및 실행

### 요구사항
- .NET 8.0 SDK

### 빌드
```bash
dotnet restore
dotnet build
```

### 테스트
```bash
dotnet test
```

## 개발 가이드

이 프로젝트는 다음 원칙을 따릅니다:
- SOLID 원칙 준수
- .NET 코딩 컨벤션 준수
- 테스트 피라미드 구조 (단위 테스트 70%, 통합 테스트 20%, E2E 테스트 10%)

자세한 개발 가이드는 `.kiro/steering/` 디렉토리의 문서를 참조하세요.

## 라이선스

MIT License