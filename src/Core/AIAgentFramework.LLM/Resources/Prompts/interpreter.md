# Interpreter 프롬프트

사용자 입력을 해석하고 의도를 파악하세요.

## 사용자 입력
{user_input}

## 컨텍스트
{context}

## 지시사항
1. 사용자 입력의 의도를 분석하세요
2. 핵심 키워드와 개념을 추출하세요
3. 명확하지 않은 부분이 있다면 질문을 제안하세요

## 응답 형식
```json
{
  "interpretation": "입력 해석 결과",
  "intent": "사용자 의도",
  "keywords": ["키워드1", "키워드2"],
  "clarification_needed": false,
  "suggested_questions": []
}
```