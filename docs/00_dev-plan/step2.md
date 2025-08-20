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

  * MCP 서버에서 제공하는 `tool.list` 엔드포인트 호출
  * Descriptor 변환 후 등록
  * 실행 시 `tool.execute` RPC 호출

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
    McpToolLoader.cs

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

