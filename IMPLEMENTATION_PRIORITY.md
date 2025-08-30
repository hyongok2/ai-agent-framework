# 구현 우선순위 매트릭스

## 🎯 의사결정 프레임워크

### 우선순위 계산 공식
```
Priority Score = (Impact × 3) + (Urgency × 2) + (Effort × -1) + (Risk × -0.5)
```

- **Impact**: 비즈니스/시스템에 미치는 영향 (1-10)
- **Urgency**: 시급성 (1-10) 
- **Effort**: 구현 난이도 (1-10, 역계산)
- **Risk**: 실패 위험성 (1-10, 역계산)

## 📊 Critical Issues 우선순위

| 이슈 | Impact | Urgency | Effort | Risk | Score | 순위 |
|------|--------|---------|--------|------|-------|------|
| 오케스트레이션 엔진 타입 안전성 | 10 | 10 | 7 | 8 | **37** | 🥇 1 |
| LLM Provider 실제 구현 | 9 | 9 | 8 | 6 | **33** | 🥈 2 |
| State Management 구현 | 8 | 7 | 9 | 7 | **26** | 🥉 3 |
| Configuration 캐시 무효화 | 6 | 8 | 4 | 3 | **26** | 4 |
| Registry 패턴 개선 | 7 | 6 | 6 | 5 | **21** | 5 |

## 🚨 즉시 착수 (Week 1)

### 1. 오케스트레이션 엔진 타입 안전성 (Score: 37)
**현재 문제**:
```csharp
// 😱 문자열 파싱에 의존
private static string GetActionType(object action) {
    return action.ToString()?.Split('_')[0] ?? "unknown";
}
```

**해결책**:
```csharp
public interface IOrchestrationAction {
    ActionType Type { get; }
    string Name { get; }
    Dictionary<string, object> Parameters { get; }
    Task<ActionResult> ExecuteAsync(IExecutionContext context);
}

// 타입 안전한 팩토리
public class ActionFactory {
    public IOrchestrationAction CreateLLMAction(string functionName, Dictionary<string, object> parameters) {
        return new LLMAction(functionName, parameters);
    }
    
    public IOrchestrationAction CreateToolAction(string toolName, Dictionary<string, object> parameters) {
        return new ToolAction(toolName, parameters);
    }
}
```

**예상 소요**: 2일
**리스크**: Low (인터페이스 변경이지만 기존 로직 단순)

### 2. LLM Provider 실제 구현 (Score: 33)
**현재 문제**:
```csharp
// 😱 가짜 토큰 카운팅
public override async Task<int> CountTokensAsync(string text) {
    return text.Length / 4; // 완전히 부정확
}
```

**해결책**:
```csharp
public class TiktokenClaudeProvider : ClaudeProvider {
    private readonly Encoding _encoding;
    
    public override int CountTokens(string text, string model) {
        return _encoding.Encode(text).Count; // 실제 토큰 계산
    }
    
    protected override async Task<LLMResponse> GenerateInternalAsync(LLMRequest request) {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages") {
            Content = new StringContent(JsonSerializer.Serialize(new {
                model = request.Model,
                max_tokens = request.MaxTokens,
                messages = new[] {
                    new { role = "user", content = request.Prompt }
                }
            }), Encoding.UTF8, "application/json")
        };
        
        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        return ParseResponse(content); // 실제 응답 파싱
    }
}
```

**예상 소요**: 3일
**리스크**: Medium (외부 API 의존성)

## ⚡ 단기 개선 (Week 2-3)

### 3. State Management 구현 (Score: 26)
**왜 중요한가**: 프로덕션 환경에서 필수
- 서버 재시작 시 세션 유지
- 분산 환경 지원
- 장애 복구 가능성

**구현 전략**:
```csharp
// 인터페이스 우선 설계
public interface IStateStore {
    Task<T> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<bool> ExistsAsync(string key);
    Task DeleteAsync(string key);
}

// 구현체는 점진적 추가
public class RedisStateStore : IStateStore { /* ... */ }
public class SqlServerStateStore : IStateStore { /* ... */ }
public class InMemoryStateStore : IStateStore { /* ... */ }
```

### 4. Configuration 캐시 무효화 (Score: 26)
**현재 문제**:
```csharp
// 😱 주석으로만 남겨둔 코드
// Note: MemoryCache doesn't have a Clear() method
// We need to implement cache key tracking for invalidation
```

**즉시 수정 가능**:
```csharp
public class ConfigurationManager {
    private readonly IMemoryCache _cache;
    private readonly ConcurrentSet<string> _cacheKeys = new();
    
    public void InvalidateCache(string pattern = null) {
        if (pattern == null) {
            // 전체 캐시 클리어
            foreach (var key in _cacheKeys.ToList()) {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key);
            }
        } else {
            // 패턴 매칭 캐시 클리어
            var keysToRemove = _cacheKeys
                .Where(key => key.Contains(pattern))
                .ToList();
                
            foreach (var key in keysToRemove) {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key);
            }
        }
    }
}
```

**예상 소요**: 0.5일
**리스크**: Very Low

## 🔧 중기 개선 (Week 4-5)

### Registry 패턴 개선 (Score: 21)
**현재 문제**: 문자열 키 기반 조회로 컴파일타임 검증 불가

**개선 방향**:
```csharp
// 타입 안전한 Registry
public interface ITypedRegistry {
    void Register<TInterface, TImplementation>() 
        where TImplementation : class, TInterface;
    T Resolve<T>() where T : class;
    IEnumerable<T> ResolveAll<T>() where T : class;
}

// 사용 예
registry.Register<ILLMFunction, PlannerFunction>();
registry.Register<ILLMFunction, AnalyzerFunction>();

var planner = registry.Resolve<PlannerFunction>(); // 컴파일타임 안전
var allFunctions = registry.ResolveAll<ILLMFunction>(); // 모든 LLM 기능
```

## 📅 구현 스케줄

### Week 1: Foundation (Critical 해결)
```
Day 1: 오케스트레이션 엔진 인터페이스 재설계
Day 2: ActionFactory 및 타입 안전 액션 구현
Day 3: LLM Provider 토큰 카운팅 구현
Day 4: LLM Provider 실제 API 호출 구현
Day 5: Configuration 캐시 무효화 + 테스트
```

### Week 2: State Management
```
Day 1: IStateStore 인터페이스 설계
Day 2: Redis 구현체 작성
Day 3: OrchestrationContext 지속성 구현
Day 4: 상태 복원 로직 구현
Day 5: 통합 테스트 작성
```

### Week 3: LLM Integration 완성
```
Day 1-2: 스트리밍 지원 구현
Day 3: Provider 팩토리 및 Fallback
Day 4: 토큰 예산 관리 시스템
Day 5: 성능 최적화
```

## 🎯 마일스톤 체크포인트

### Week 1 완료 체크리스트
- [ ] `GetActionType(object action)` 메서드 완전 제거
- [ ] 모든 액션이 `IOrchestrationAction` 구현
- [ ] ClaudeProvider 실제 API 호출 작동
- [ ] 실제 토큰 카운팅 정확도 95% 이상
- [ ] Configuration 캐시 무효화 기능 테스트

### Week 2 완료 체크리스트
- [ ] Redis 연결 시 상태 저장/복원 100% 작동
- [ ] 서버 재시작 후 세션 복원 성공
- [ ] 분산 환경에서 상태 공유 가능
- [ ] 상태 저장 성능 < 10ms (P95)

### Week 3 완료 체크리스트
- [ ] 스트리밍 응답 실시간 처리
- [ ] Provider 장애 시 자동 Failover
- [ ] 토큰 예산 초과 시 적절한 처리
- [ ] E2E 테스트 통과율 95% 이상

## 💡 Risk Mitigation Strategy

### High Risk: LLM API 통합
**위험**: 외부 API 변경, 네트워크 이슈
**대응**: 
- Mock Provider로 우선 개발
- Circuit Breaker 패턴 적용
- 여러 Provider 동시 지원

### Medium Risk: State Management 성능
**위험**: Redis 성능, 네트워크 지연
**대응**:
- 로컬 캐시와 2-tier 구조
- 비동기 상태 저장
- 상태 압축 적용

### Low Risk: 기존 코드 호환성
**위험**: 인터페이스 변경으로 인한 영향
**대응**:
- Adapter 패턴으로 점진적 마이그레이션
- 충분한 테스트 커버리지
- 버전별 지원 계획

## 📈 성공 지표

### 기술적 지표
- 컴파일 에러: 0개 유지
- 테스트 통과율: 95% 이상
- 코드 커버리지: 80% 이상
- 성능 저하: 10% 이내

### 비즈니스 지표  
- 프로덕션 배포 가능성: Yes
- 확장성: 10배 트래픽 대응 가능
- 안정성: 99.9% 가용성
- 개발자 경험: 만족도 9/10 이상

이 우선순위를 따르면 **가장 임팩트가 큰 문제부터 체계적으로 해결**하여 효율적인 리팩토링이 가능합니다.