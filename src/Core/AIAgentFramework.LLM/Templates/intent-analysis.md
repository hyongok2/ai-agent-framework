# Intent Analysis

## User Input
{{{USER_INPUT}}}

{{#if HISTORY}}## Conversation History
{{HISTORY}}{{/if}}

{{#if CONTEXT}}## Context
{{CONTEXT}}{{/if}}

{{#if TOOLS}}## Available Tools
{{TOOLS}}{{/if}}

{{#if LLM_FUNCTIONS}}## Available LLM Functions
{{LLM_FUNCTIONS}}{{/if}}

## Task

Analyze user intent and respond immediately if possible. Use the available tools and LLM functions information to better determine if the request requires task execution (Task) or can be answered directly (Chat/Question).

**Intent Types:**
1. **Chat**: Greeting, thanks, casual conversation
   - Examples: "안녕", "고마워", "잘했어"
   - Action: Generate friendly direct response

2. **Question**: Information or explanation request
   - Examples: "AI가 뭐야?", "어떻게 작동해?", "설명해줘"
   - Action: Generate informative answer

3. **Task**: Action/tool execution required
   - Examples: "파일 읽어줘", "요약해줘", "실행해줘"
   - Action: Set needsPlanning=true

**Output Format - JSON ONLY (no markdown, no explanation):**

Chat example:
{"intentType":"Chat","needsPlanning":false,"directResponse":"안녕하세요! 무엇을 도와드릴까요?","confidence":0.95}

Question example:
{"intentType":"Question","needsPlanning":false,"directResponse":"AI는 인공지능으로...","confidence":0.9}

Task example:
{"intentType":"Task","needsPlanning":true,"taskDescription":"파일 읽고 요약","confidence":0.95}

**Critical: Output ONLY the JSON object. No ```json tags, no extra text.**
