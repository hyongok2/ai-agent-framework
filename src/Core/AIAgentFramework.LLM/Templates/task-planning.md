# Task Planning

## Tools
{{TOOLS}}

## LLM Functions
{{LLM_FUNCTIONS}}

## Request
{{{USER_REQUEST}}}

{{#if CONTEXT}}## Context
{{CONTEXT}}{{/if}}

{{#if HISTORY}}## History
{{HISTORY}}{{/if}}

{{#if PREVIOUS_RESULTS}}## Previous Results
{{PREVIOUS_RESULTS}}{{/if}}

## Task

Classify request type and create plan:

**Types:**
- **ToolExecution**: 파일/명령/데이터 작업 필요
- **SimpleResponse**: 인사/감사/단순 대화
- **Information**: 설명/지식 질문
- **Clarification**: 불명확, 추가정보 필요

**Output JSON:**
```json
{
  "type": "ToolExecution|SimpleResponse|Information|Clarification",
  "summary": "작업 설명",
  "directResponse": "직접 응답 (non-ToolExecution만)",
  "steps": [
    {
      "stepNumber": 1,
      "description": "단계 설명",
      "toolName": "도구명",
      "parameters": "파라미터",
      "outputVariable": "변수명",
      "dependsOn": [],
      "estimatedSeconds": 5
    }
  ],
  "totalEstimatedSeconds": 30,
  "isExecutable": true,
  "executionBlocker": null,
  "constraints": []
}
```

**Rules:**
- 이전 단계 결과 참조: `{variableName}`
- 사용 가능한 도구만 사용
- 실행 불가시 `isExecutable: false` + `executionBlocker` 설명
