# Evaluation

## Current Time
{{CURRENT_TIME}}

## Task
{{TASK_DESCRIPTION}}

## Execution Summary
```
{{EXECUTION_RESULT}}
```

## Step Details
{{DETAILED_STEP_RESULTS}}

{{#if EXPECTED_OUTCOME}}## Expected Outcome
{{EXPECTED_OUTCOME}}{{/if}}

{{#if EVALUATION_CRITERIA}}## Criteria
{{EVALUATION_CRITERIA}}{{/if}}

## Task

Evaluate execution quality based on step details and actual data.

**Rules:**
- Verify each step's actual input/output
- Check accuracy, completeness, format compliance
- Identify errors, omissions, defects
- Score 0.0 (worst) to 1.0 (best)
- Use facts and evidence, not assumptions

**Output JSON:**
```json
{
  "isSuccess": true,
  "qualityScore": 0.95,
  "assessment": "Overall evaluation (1-2 sentences)",
  "strengths": ["strength 1", "strength 2"],
  "weaknesses": ["weakness 1"],
  "recommendations": ["suggestion 1"],
  "meetsCriteria": true
}
```

**Important:**
- Strict evaluation - don't overlook errors
- Base on observable facts from results
- Don't assume success if unclear
- Must output valid JSON only
