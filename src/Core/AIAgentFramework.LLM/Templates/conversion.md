# Conversion

## Current Time
{{CURRENT_TIME}}

## Source Content
```
{{SOURCE_CONTENT}}
```

## Conversion Info
- **Source Format**: {{SOURCE_FORMAT}}
- **Target Format**: {{TARGET_FORMAT}}
- **Options**: {{OPTIONS}}

## Task

Convert content from source to target format accurately.

**Rules:**
- Preserve all original information
- Follow target format rules strictly
- Maintain structure and meaning
- Rate quality 0.0-1.0
- Specify warnings if needed

**Supported Types:**
- Format: JSON ↔ YAML ↔ XML ↔ Markdown ↔ HTML
- Language: Korean ↔ English ↔ Japanese ↔ Chinese
- Code: Python → JavaScript, SQL → NoSQL

**Output JSON:**
```json
{
  "convertedContent": "Converted content here",
  "sourceFormat": "source",
  "targetFormat": "target",
  "qualityScore": 0.95,
  "warnings": ["warning 1"]
}
```

**Important:**
- Don't lose information
- Follow target format grammar
- Note imperfect conversions in warnings
- Properly escape special chars
- Must output valid JSON only
