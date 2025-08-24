# 📍 2단계: 도구 계층 (Tools Layer)

---

## 🎯 목표

* 다양한 형태의 도구(내장, 플러그인, MCP)를 \*\*동일 인터페이스(`ITool`)\*\*로 실행 가능하게 만들기
* 모든 도구를 중앙에서 관리할 수 있는 **Tool Registry** 구축
* 도구마다 다른 Config를 **분리 파일로 관리**, 필요시 Secret Resolver를 통해 안전하게 불러오기

---

## 📦 주요 구성 요소

### 1. Tool Descriptor 스키마

* 도구의 “정체성(ID) + I/O 스키마 + 메타데이터”
* **Provider/Namespace/Name/Version**으로 유일 식별

```csharp
public sealed record ToolDescriptor(
    string Provider,       // "internal", "plugin", "mcp"
    string Namespace,      // "math", "sql", "http"
    string Name,           // "calculate", "query"
    string Version,        // "1.0.0"
    JsonNode InputSchema,  // JSON Schema
    JsonNode OutputSchema
);
```

---

### 2. Tool Registry

* 모든 도구를 등록/조회/실행할 수 있는 허브
* Key = `"provider/namespace/name/version"`

```csharp
public interface IToolRegistry
{
    void Register(ITool tool);
    ITool? Resolve(string provider, string ns, string name, string version = "latest");
    IEnumerable<ToolDescriptor> ListAll();
}
```

* 구현 예시: `ToolRegistry` 내부에서 Dictionary 관리

---

### 3. 도구 로더 (Dynamic Loader)

* **내장 도구 (Built-in)**: 프로젝트에 직접 포함된 기본 도구

  * 예: `MathTool`, `HttpTool`, `FileTool`
* **플러그인 (Plugin)**: DLL/Assembly 스캔 → `ITool` 구현체 자동 등록
* **MCP 도구 (Model Context Protocol)**:

  * MCP 서버에서 제공하는 `tool.list` 엔드포인트 호출 (HTTP/WebSocket 클라이언트 사용)
  * Descriptor 변환 후 등록
  * 실행 시 `tool.execute` RPC 호출
  * C# 구현: `HttpClient` 또는 `ClientWebSocket`을 통한 JSON-RPC 통신

```csharp
public interface IToolLoader
{
    Task<IEnumerable<ITool>> LoadAsync(string source, CancellationToken ct);
}
```

---

### 4. Config Provider

* 도구마다 서로 다른 Config (DB 접속, API Key, 엔드포인트 등)을 **분리 JSON 파일**로 관리
* 구조 예시:

```
configs/tools/
  sql.defaults.json
  http.defaults.json

configs/instances/dev.json
configs/instances/prod.json
```

* `sql.defaults.json`

```json
{
  "connectionString": "secret://db/main",
  "timeout": 30
}
```

* `ConfigProvider`는 도구 실행 전에 이 설정을 주입

```csharp
public interface IConfigProvider
{
    JsonNode GetConfig(string toolName, string environment);
}
```

---

### 5. Secret Resolver

* Config 내 `secret://...` 플레이스홀더를 안전하게 해석
* 소스: 환경변수, Vault, OS Keychain 등

```csharp
public interface ISecretResolver
{
    string Resolve(string secretRef);
}
```

---

### 6. ToolContext

* 도구 실행에 필요한 공통 정보 (환경, 로깅, 트레이스)

```csharp
public sealed record ToolContext(
    RunId RunId,
    StepId StepId,
    string Environment,
    IDictionary<string, object> Metadata
);
```

---

## 📂 디렉토리 배치 (2단계 산출물)

```
src/Agent.Tools.Abstractions/
  IToolRegistry.cs
  IToolLoader.cs
  IConfigProvider.cs
  ISecretResolver.cs
  ToolContext.cs

src/Agent.Tools.Core/
  BuiltIn/
    MathTool.cs
    HttpTool.cs
  Registry/
    ToolRegistry.cs
  Config/
    JsonConfigProvider.cs
    EnvSecretResolver.cs
  Loader/
    PluginToolLoader.cs
    McpToolLoader.cs     // MCP는 HTTP/WebSocket 클라이언트로 구현

configs/tools/
  math.defaults.json
  http.defaults.json
  sql.defaults.json

configs/instances/
  dev.json
  prod.json
```

---

## ✅ 2단계 완료 기준

* [ ] `ToolDescriptor` 스키마 정의 완료
* [ ] `IToolRegistry` 기본 구현 (등록/조회/목록)
* [ ] Built-in 도구 최소 2개(`math`, `http`) 제공
* [ ] Plugin Loader로 DLL에서 `ITool` 자동 등록 가능
* [ ] MCP Tool Loader가 원격 MCP 서버에서 `tool.list` → 등록 가능
* [ ] `ConfigProvider`가 기본 Config + 환경별 Override 제공



# 📚 **AI Agent Framework - Tool Layer 구현 가이드**

## 1. **개요**

### 목표
- **통합 인터페이스**: 내장 도구, 플러그인, MCP 도구를 동일한 `ITool` 인터페이스로 실행
- **중앙 관리**: Tool Registry를 통한 모든 도구의 등록/조회/실행 관리
- **설정 분리**: 환경별 설정과 Secret을 안전하게 관리
- **확장성**: 새로운 도구 소스를 쉽게 추가할 수 있는 구조

### 핵심 원칙
- 모든 도구는 `ITool` 인터페이스를 구현
- 도구 식별은 `Provider/Namespace/Name/Version` 4차원 체계 사용
- 입출력은 JSON Schema로 검증
- 실행 컨텍스트(`ToolContext`)를 통해 추적 정보 전달

---

## 2. **Core Tools 계층**

### 2.1 **Tool Registry**
- **역할**: 모든 도구의 중앙 저장소
- **기능**:
  - 도구 등록/해제
  - ID 기반 조회
  - 검색 (카테고리, 태그, 기능별)
  - 버전 관리 (최신 버전 자동 선택)
- **구현 요구사항**:
  - Thread-safe 구현 (ConcurrentDictionary)
  - 도구 초기화 시 설정 자동 주입
  - 중복 등록 방지

### 2.2 **Tool Loader 시스템**
- **IToolLoader 인터페이스**: 다양한 소스에서 도구를 로드하는 통합 인터페이스
- **구현체 종류**:
  - **BuiltInLoader**: 코드에 하드코딩된 내장 도구
  - **PluginLoader**: DLL 파일에서 동적 로드
  - **McpLoader**: MCP 서버에서 도구 가져오기
- **로더별 특징**:
  - 각 로더는 `SourceType` 속성으로 구분
  - 초기화 시 모든 로더 순회하며 도구 수집
  - 로더는 설정을 받아 도구 초기화 지원

### 2.3 **Configuration Provider**
- **목적**: 도구별 설정을 환경에 따라 관리
- **설정 계층 구조**:
  ```
  1. 기본 설정 (tools/{namespace}.defaults.json)
  2. 환경별 설정 (instances/{environment}.json)
  3. 런타임 오버라이드
  ```
- **병합 전략**: Deep merge로 환경별 설정이 기본값 오버라이드
- **설정 예시**:
  - 타임아웃, 재시도 횟수
  - API 엔드포인트, 연결 문자열
  - 기능 활성화/비활성화 플래그

### 2.4 **Secret Resolver**
- **목적**: 민감한 정보를 설정에서 분리
- **지원 패턴**:
  - `secret://vault/key` - Vault 통합
  - `env://VARIABLE` - 환경 변수
  - `${VARIABLE}` - 프리픽스 기반 환경 변수
- **해석 과정**:
  1. 설정 로드 시 Secret 참조 감지
  2. 참조를 실제 값으로 치환
  3. 치환된 설정을 도구에 전달
- **보안 고려사항**:
  - Secret은 메모리에만 유지
  - 로그에 Secret 노출 방지
  - 실패 시 명확한 에러 (Secret 값은 노출하지 않음)

### 2.5 **내장 도구 구현**

#### **MathTool**
- **기능**: 수학 표현식 평가
- **입력**: 표현식 문자열, 변수 딕셔너리
- **보안**: 위험한 키워드 차단, 표현식 길이 제한
- **특징**: 멱등성, 캐시 가능

#### **HttpTool**
- **기능**: HTTP 요청 실행
- **입력**: Method, URL, Headers, Body
- **보안**: Private 네트워크 차단 옵션
- **특징**: 스트리밍 지원 (SSE), 타임아웃 설정

#### **FileTool**
- **기능**: 파일 시스템 작업
- **입력**: 경로, 작업 타입
- **보안**: 샌드박스 경로 제한
- **특징**: 대용량 파일 스트리밍

---

## 3. **MCP (Model Context Protocol) 통합**

### 3.1 **MCP 개요**
- **정의**: Anthropic이 제안한 AI 도구 표준 프로토콜
- **제공 기능**:
  - Tools: 실행 가능한 도구
  - Resources: 읽기 가능한 데이터
  - Prompts: 재사용 가능한 프롬프트 템플릿
- **통신 방식**: JSON-RPC 2.0 over stdio/HTTP/WebSocket

### 3.2 **MCP Client**
- **역할**: MCP 서버와의 통신 담당
- **생명주기**:
  1. Connect → Initialize → Ready
  2. List Tools/Resources/Prompts
  3. Execute/Read 요청
  4. Disconnect
- **연결 관리**:
  - 자동 재연결 지원
  - 연결 상태 모니터링
  - 타임아웃 처리

### 3.3 **Transport 계층**
- **Stdio Transport**:
  - 프로세스 생성 및 관리
  - 표준 입출력 통신
  - 사용 예: Node.js/Python MCP 서버
- **HTTP Transport**:
  - REST API 스타일 통신
  - 상태 없는 요청/응답
  - 사용 예: 웹 서비스 MCP
- **WebSocket Transport**:
  - 양방향 실시간 통신
  - 서버 푸시 알림 지원
  - 사용 예: 실시간 데이터 MCP

### 3.4 **MCP Tool Proxy**
- **역할**: MCP 도구를 ITool 인터페이스로 래핑
- **변환 과정**:
  1. MCP 도구 정의 → ToolDescriptor
  2. MCP 입력 스키마 → ITool InputSchema
  3. MCP 실행 결과 → ToolResult
- **에러 처리**:
  - MCP 에러 코드를 ToolResult 에러로 매핑
  - 연결 실패 시 재시도 또는 실패 반환

### 3.5 **MCP 설정 관리**
- **서버 설정**:
  ```json
  {
    "name": "서버 이름",
    "transport": "stdio|http|websocket",
    "command": "실행 명령 (stdio)",
    "url": "서버 주소 (http/ws)",
    "environment": "환경 변수"
  }
  ```
- **다중 서버 지원**: 여러 MCP 서버 동시 연결
- **서버별 기능**: Capabilities 조회 후 선택적 사용

---

## 4. **통합 및 실행 흐름**

### 4.1 **초기화 과정**
1. **Registry 생성**: DefaultToolRegistry 인스턴스화
2. **로더 등록**: 각 IToolLoader 구현체 등록
3. **도구 로드**:
   - 각 로더에서 도구 수집
   - 설정 로드 및 Secret 해석
   - 도구 초기화
   - Registry에 등록
4. **준비 완료**: 도구 사용 가능 상태

### 4.2 **도구 실행 과정**
1. **요청 수신**: 도구 ID와 입력 데이터
2. **도구 조회**: Registry에서 도구 검색
3. **입력 검증**: JSON Schema 검증
4. **컨텍스트 생성**: RunId, StepId, 환경 정보
5. **실행**: tool.ExecuteAsync() 호출
6. **결과 반환**: ToolResult (성공/실패, 메트릭)

### 4.3 **스트리밍 실행**
- **IStreamingTool**: 점진적 결과 반환
- **청크 타입**:
  - Status: 상태 업데이트
  - Progress: 진행률
  - Data: 부분 결과
  - Final: 최종 결과
- **사용 사례**: 
  - 대용량 데이터 처리
  - 실시간 피드백
  - 긴 실행 작업

---

## 5. **확장 포인트**

### 5.1 **새로운 도구 추가**
- ITool 인터페이스 구현
- ToolDescriptor 정의
- 선택적: IStreamingTool 구현
- 플러그인 DLL로 패키징

### 5.2 **새로운 로더 추가**
- IToolLoader 구현
- 고유한 SourceType 정의
- LoadAsync에서 도구 수집 로직

### 5.3 **새로운 Secret 소스**
- ISecretResolver 구현
- 패턴 매칭 로직
- 실제 값 조회 구현

---

## 6. **구현 체크리스트**

### Phase 1: 기본 구조 (1주)
- [ ] IToolLoader, IToolConfigProvider, ISecretResolver 인터페이스 추가
- [ ] DefaultToolRegistry 구현
- [ ] JsonConfigProvider 구현
- [ ] EnvSecretResolver 구현

### Phase 2: 내장 도구 (3-4일)
- [ ] MathTool 구현
- [ ] HttpTool 구현
- [ ] FileTool 구현
- [ ] 도구별 설정 파일 템플릿

### Phase 3: 플러그인 지원 (2-3일)
- [ ] PluginToolLoader 구현
- [ ] DLL 스캔 및 로드
- [ ] 의존성 주입 지원

### Phase 4: MCP 통합 (1-2주)
- [ ] MCP Client 구현
- [ ] Transport 구현 (stdio, http, websocket)
- [ ] McpToolProxy 구현
- [ ] McpToolLoader 구현

### Phase 5: 테스트 및 문서화 (3-4일)
- [ ] 단위 테스트
- [ ] 통합 테스트
- [ ] 성능 테스트
- [ ] 사용 가이드 문서

---

## 7. **주요 고려사항**

### 보안
- 도구 실행 권한 관리
- 입력 검증 필수
- Secret 노출 방지
- 샌드박스 실행 환경

### 성능
- 도구 초기화 지연 로딩
- 결과 캐싱 (멱등 도구)
- 병렬 실행 지원
- 메모리 사용량 모니터링

### 운영
- 도구 버전 관리
- 설정 핫 리로드
- 에러 추적 및 로깅
- 메트릭 수집

### 확장성
- 플러그인 아키텍처
- 다양한 도구 소스 지원
- 커스텀 로더 추가 가능
- 프로토콜 독립적 설계
