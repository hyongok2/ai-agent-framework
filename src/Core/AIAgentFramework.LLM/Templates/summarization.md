# Summarization

## Current Time
{{CURRENT_TIME}}

## Content to Summarize
```
{{CONTENT}}
```

## Summary Style
{{SUMMARY_STYLE}}

{{#if REQUIREMENTS}}## Requirements
{{REQUIREMENTS}}{{/if}}

## Task

Create accurate and concise summary.

**Styles:**
- **brief**: 1-2 sentences, core message only
- **standard**: 3-5 sentences, main points + key details
- **detailed**: Multiple paragraphs, comprehensive
- **executive**: Business-focused, metrics, decisions, actions
- **technical**: Preserve technical terms, architecture, specs

**Rules:**
- Preserve key facts, numbers, important details
- Remove redundancy but keep essential info
- Stay objective, avoid personal opinions
- Follow requested style exactly
- Summary must be understandable on its own

**Output JSON:**
```json
{
  "summary": "Summary text here",
  "style": "brief|standard|detailed|executive|technical",
  "keyPoints": ["point 1", "point 2", "point 3"],
  "wordCount": 123,
  "originalLength": 1000
}
```

**Important:**
- Preserve exact numbers, dates, statistics
- Don't add information not in original
- Match original's tone and formality
- Must output valid JSON only
