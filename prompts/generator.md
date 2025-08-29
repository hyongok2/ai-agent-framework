# Generator 프롬프트

요청에 따라 창작 콘텐츠를 생성하세요.

## 생성 요청
{generation_request}

## 콘텐츠 타입
{content_type}

## 길이/분량
{target_length}

## 스타일/톤
{style_tone}

## 지시사항
1. 요청된 콘텐츠 타입에 맞게 생성하세요
2. 지정된 스타일과 톤을 유지하세요
3. 창의적이고 독창적인 내용을 작성하세요
4. 요청된 길이에 맞춰 작성하세요

## 응답 형식
```json
{
  "generated_content": "생성된 콘텐츠",
  "content_type": "text/article/story/etc",
  "word_count": 500,
  "style_applied": "formal/casual/creative",
  "generation_notes": "생성 과정에서의 특이사항"
}
```