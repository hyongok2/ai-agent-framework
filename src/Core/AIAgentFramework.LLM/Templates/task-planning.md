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

{{#if ITERATION_NUMBER}}{{#if PREVIOUS_ATTEMPT_SUMMARY}}## Previous Attempt (Iteration {{ITERATION_NUMBER}})

**Previous Execution Summary:**
{{PREVIOUS_ATTEMPT_SUMMARY}}

{{#if EVALUATION_FEEDBACK}}**Quality Evaluation Feedback:**
{{EVALUATION_FEEDBACK}}{{/if}}

{{#if FAILURE_REASON}}**Failure Reasons:**
{{FAILURE_REASON}}{{/if}}

**Instructions for Re-planning:**
- Analyze why the previous attempt failed or scored low
- Address the specific weaknesses identified in the evaluation
- Adjust your strategy based on the feedback
- Improve tool selection, parameter configuration, or step ordering
- Ensure the new plan resolves the issues from the previous iteration
{{/if}}{{/if}}

## Task

Create execution plan using available tools and Universal LLM.

**Tool vs Universal:**
- **Tools**: File read/write, shell command, directory operations
- **Universal**: Text analysis, summarization, translation, extraction, Q&A

**Universal LLM Usage (REQUIRED for text processing):**
- toolName: "Universal"
- parameters: { "taskType": "analyze|summarize|translate|extract|answer", "content": "{data}" }
- responseGuide: ALWAYS include { "instruction": "what to do", "format": "JSON|Text", "style": "concise" }

**Output JSON:**
```json
{
  "type": "ToolExecution",
  "summary": "Plan summary",
  "steps": [
    {
      "stepNumber": 1,
      "description": "Read file",
      "toolName": "FileReader",
      "parameters": {"path": "file.txt"},
      "outputVariable": "content",
      "dependsOn": [],
      "estimatedSeconds": 5
    },
    {
      "stepNumber": 2,
      "description": "Summarize content",
      "toolName": "Universal",
      "parameters": {"taskType": "summarize", "content": "{content}"},
      "outputVariable": "summary",
      "dependsOn": [1],
      "estimatedSeconds": 10,
      "responseGuide": {
        "instruction": "Create brief summary",
        "format": "Text",
        "style": "concise"
      }
    }
  ],
  "totalEstimatedSeconds": 30,
  "isExecutable": true
}
```

**Rules:**
- Reference previous step output: `{varName}`
- dependsOn: Use step numbers [1, 2], not variable names
- Universal steps MUST have responseGuide
- Output JSON only, no markdown
