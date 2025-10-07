# Extraction

## Current Time
{{CURRENT_TIME}}

## Source Text
```
{{SOURCE_TEXT}}
```

## Extraction Info
- **Type**: {{EXTRACTION_TYPE}}
- **Criteria**: {{CRITERIA}}

## Task

Extract all information of specified type from text.

**Rules:**
- Extract all items completely
- Values must exist in original text
- Provide context for each item
- Remove duplicates
- Rate confidence 0.0-1.0

**Supported Types:**
- **entities**: Person, organization, location, product
- **dates**: Dates, times, periods
- **contacts**: Emails, phones
- **keywords**: Key terms
- **numbers**: Numbers, amounts, stats
- **urls**: Web addresses
- **custom**: User-defined patterns

**Output JSON:**
```json
{
  "extractedItems": [
    {
      "value": "extracted value",
      "type": "item subtype",
      "context": "surrounding text"
    }
  ],
  "extractionType": "type",
  "totalCount": 3,
  "confidence": 0.95
}
```

**Important:**
- Values must actually exist in text
- Provide context for each item
- Remove duplicates
- Must output valid JSON only
