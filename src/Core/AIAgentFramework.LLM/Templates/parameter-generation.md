# Parameter Generation

## User Request
{{{USER_REQUEST}}}

## Current Step
**Description**: {{{STEP_DESCRIPTION}}}

## Target Tool
**Name**: {{{TOOL_NAME}}}

**Schema**:
```json
{{{TOOL_INPUT_SCHEMA}}}
```

{{#if PREVIOUS_RESULTS}}## Previous Results
{{{PREVIOUS_RESULTS}}}{{/if}}

{{#if CONVERSATION_HISTORY}}## Conversation History
{{{CONVERSATION_HISTORY}}}{{/if}}

{{#if ADDITIONAL_CONTEXT}}## Additional Context
{{{ADDITIONAL_CONTEXT}}}{{/if}}

## Task

Generate exact parameters for tool execution.

**Rules:**
- Follow schema strictly
- Extract actual values from previous results, not placeholders
- Use absolute paths
- Match types exactly (string/boolean/number/object/array)
- Include all required fields
- Escape JSON properly

**Output JSON:**
```json
{
  "toolName": "ToolName",
  "parameters": "JSON string or value",
  "reasoning": "Brief explanation",
  "isValid": true,
  "errorMessage": null
}
```

**Path Format:** Windows paths in JSON use `\\\\` (e.g., `"c:\\\\data"`)
