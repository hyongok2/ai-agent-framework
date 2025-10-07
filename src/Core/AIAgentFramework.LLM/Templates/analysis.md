# Analysis

## Current Time
{{CURRENT_TIME}}

## Content
```
{{CONTENT}}
```

## Purpose
{{PURPOSE}}

{{#if FOCUS_AREA}}## Focus Area
{{FOCUS_AREA}}{{/if}}

## Task

Analyze text deeply: intent, key info, sentiment, ambiguity.

**Rules:**
- Identify main intent/purpose
- Extract key entities (names, dates, keywords)
- Determine sentiment (positive/negative/neutral)
- Rate confidence 0.0-1.0
- Flag ambiguities

**Output JSON:**
```json
{
  "intent": "Main purpose (one sentence)",
  "entities": ["entity1", "entity2"],
  "sentiment": "positive|negative|neutral",
  "confidence": 0.85,
  "detailedAnalysis": "2-3 sentences",
  "ambiguities": ["unclear point 1"]
}
```

**Important:**
- Lower confidence if uncertain
- Extract only entities actually in text
- Must output valid JSON only
