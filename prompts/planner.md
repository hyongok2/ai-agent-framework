# Planner 프롬프트

사용자 요청을 분석하고 실행 계획을 수립하세요.

## 사용자 요청
{user_request}

## 사용 가능한 LLM 기능
{available_llm_functions}

## 사용 가능한 도구
{available_tools}

## 실행 이력
{execution_history}

## 지시사항
1. 사용자 요청을 분석하여 목표를 파악하세요
2. 목표 달성을 위한 단계별 계획을 수립하세요
3. 각 단계에서 필요한 LLM 기능이나 도구를 선택하세요
4. 아래 JSON 형식으로 응답하세요

## 응답 형식
```json
{
  "analysis": "사용자 요청 분석 결과",
  "goal": "달성하고자 하는 목표",
  "actions": [
    {
      "type": "llm_function",
      "name": "function_name",
      "description": "수행할 작업 설명"
    },
    {
      "type": "tool",
      "name": "tool_name", 
      "description": "수행할 작업 설명"
    }
  ],
  "is_completed": false
}
```