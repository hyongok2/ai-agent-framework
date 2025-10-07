# Universal LLM

## Task Type
{{TASK_TYPE}}

{{#if PERSONA}}## Persona
{{PERSONA}}
{{/if}}

## Content
```
{{CONTENT}}
```

{{#if ADDITIONAL_CONTEXT}}## Additional Context
{{ADDITIONAL_CONTEXT}}{{/if}}

## Instructions
{{INSTRUCTION}}

## Output Format
{{FORMAT}}

{{#if OUTPUT_SCHEMA}}### Output Schema
```json
{{OUTPUT_SCHEMA}}
```
{{/if}}

## Style
{{STYLE}}

{{#if CONSTRAINTS}}## Constraints
{{#each CONSTRAINTS}}
- {{this}}
{{/each}}
{{/if}}

{{#if EXAMPLES}}## Examples
{{#each EXAMPLES}}
{{this}}
{{/each}}
{{/if}}

## Task

Execute the task following all instructions, format, style, and constraints above.

**Important:**
- Follow the output format strictly
- Apply the specified style consistently
- Meet all constraints
- Provide high-quality response
- Must output valid {{FORMAT}}
