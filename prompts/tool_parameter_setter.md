# Tool Parameter Setter 프롬프트

도구 실행을 위한 파라미터를 설정하세요.

## 사용자 요청
{user_request}

## 도구 정보
- 도구명: {tool_name}
- 설명: {tool_description}

## 도구 스키마
### 입력 스키마
{input_schema}

### 필수 파라미터
{required_parameters}

### 선택적 파라미터
{optional_parameters}

## 지시사항
1. 사용자 요청을 분석하여 도구 실행에 필요한 파라미터를 추출하세요
2. 필수 파라미터는 반드시 포함해야 합니다
3. 적절한 기본값을 설정하세요
4. 아래 JSON 형식으로 응답하세요

## 응답 형식
```json
{
  "success": true,
  "parameters": {
    "param1": "value1",
    "param2": "value2"
  },
  "reasoning": "파라미터 설정 이유"
}
```