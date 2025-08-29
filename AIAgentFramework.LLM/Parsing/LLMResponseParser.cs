using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIAgentFramework.LLM.Parsing;

/// <summary>
/// LLM 응답 파서 구현
/// </summary>
public class LLMResponseParser : ILLMResponseParser
{
    private readonly ILogger<LLMResponseParser> _logger;
    private readonly Regex _jsonBlockRegex;
    private readonly Regex _jsonObjectRegex;
    private readonly Regex _userQuestionRegex;
    private readonly Regex _errorPatternRegex;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public LLMResponseParser(ILogger<LLMResponseParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 정규식 패턴 컴파일
        _jsonBlockRegex = new Regex(@"```json\s*(.*?)\s*```", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        _jsonObjectRegex = new Regex(@"\{.*\}", 
            RegexOptions.Singleline | RegexOptions.Compiled);
        
        _userQuestionRegex = new Regex(@"(질문|question|ask|확인|clarify|need more|추가.*정보)", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        _errorPatternRegex = new Regex(@"(error|오류|실패|failed|exception|문제)", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <inheritdoc />
    public T? Parse<T>(string response) where T : class
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogWarning("Empty response provided for parsing");
            return null;
        }

        try
        {
            // 응답 타입 감지
            var responseType = DetectResponseType(response);
            
            return responseType switch
            {
                LLMResponseType.StructuredJson => ParseJson<T>(response),
                LLMResponseType.PlainText => ParsePlainText<T>(response),
                LLMResponseType.UserQuestion => ParseUserQuestion<T>(response),
                LLMResponseType.Error => ParseError<T>(response),
                LLMResponseType.Completion => ParseCompletion<T>(response),
                LLMResponseType.SpecialFunction => ParseSpecialFunction<T>(response),
                _ => ParseJson<T>(response) // 기본적으로 JSON 파싱 시도
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse response of type {Type}", typeof(T).Name);
            return null;
        }
    }

    /// <inheritdoc />
    public T? ParseJson<T>(string response) where T : class
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            var jsonContent = ExtractJsonBlock(response);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("No JSON content found in response");
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var result = JsonSerializer.Deserialize<T>(jsonContent, options);
            
            if (result != null)
            {
                _logger.LogDebug("Successfully parsed JSON response to type {Type}", typeof(T).Name);
            }
            
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing failed for type {Type}. Content: {Content}", 
                typeof(T).Name, response.Length > 200 ? response.Substring(0, 200) + "..." : response);
            return null;
        }
    }

    /// <inheritdoc />
    public bool ValidateStructuredResponse(string response, params string[] expectedFields)
    {
        if (string.IsNullOrWhiteSpace(response) || expectedFields == null || expectedFields.Length == 0)
            return false;

        try
        {
            var jsonContent = ExtractJsonBlock(response);
            if (string.IsNullOrWhiteSpace(jsonContent))
                return false;

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            foreach (var field in expectedFields)
            {
                if (!root.TryGetProperty(field, out _))
                {
                    _logger.LogWarning("Required field '{Field}' not found in response", field);
                    return false;
                }
            }

            _logger.LogDebug("Validated structured response with {FieldCount} required fields", expectedFields.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate structured response");
            return false;
        }
    }

    /// <inheritdoc />
    public string ExtractJsonBlock(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        try
        {
            // 1. JSON 코드 블록 찾기 (```json ... ```)
            var jsonBlockMatch = _jsonBlockRegex.Match(response);
            if (jsonBlockMatch.Success)
            {
                var content = jsonBlockMatch.Groups[1].Value.Trim();
                _logger.LogDebug("Extracted JSON from code block");
                return content;
            }

            // 2. 직접 JSON 객체 찾기 ({ ... })
            var jsonObjectMatch = _jsonObjectRegex.Match(response);
            if (jsonObjectMatch.Success)
            {
                var content = jsonObjectMatch.Value;
                _logger.LogDebug("Extracted JSON object directly");
                return content;
            }

            // 3. 전체 응답이 JSON인지 확인
            if (response.Trim().StartsWith("{") && response.Trim().EndsWith("}"))
            {
                _logger.LogDebug("Using entire response as JSON");
                return response.Trim();
            }

            _logger.LogWarning("No JSON content found in response");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract JSON block from response");
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public LLMResponseType DetectResponseType(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return LLMResponseType.PlainText;

        try
        {
            var lowerResponse = response.ToLowerInvariant();

            // JSON 구조화된 응답 감지
            if (_jsonBlockRegex.IsMatch(response) || 
                (response.Trim().StartsWith("{") && response.Trim().EndsWith("}")))
            {
                return LLMResponseType.StructuredJson;
            }

            // 사용자 질문 감지
            if (_userQuestionRegex.IsMatch(response))
            {
                return LLMResponseType.UserQuestion;
            }

            // 오류 응답 감지
            if (_errorPatternRegex.IsMatch(response))
            {
                return LLMResponseType.Error;
            }

            // 완료 응답 감지
            if (lowerResponse.Contains("완료") || lowerResponse.Contains("complete") || 
                lowerResponse.Contains("finished") || lowerResponse.Contains("done"))
            {
                return LLMResponseType.Completion;
            }

            // 특수 기능 호출 감지
            if (lowerResponse.Contains("function_call") || lowerResponse.Contains("tool_call") ||
                lowerResponse.Contains("special_action"))
            {
                return LLMResponseType.SpecialFunction;
            }

            return LLMResponseType.PlainText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect response type");
            return LLMResponseType.PlainText;
        }
    }

    /// <summary>
    /// 일반 텍스트 파싱
    /// </summary>
    private T? ParsePlainText<T>(string response) where T : class
    {
        // 일반 텍스트를 특정 타입으로 변환하는 로직
        if (typeof(T) == typeof(string))
        {
            return response as T;
        }

        // 다른 타입의 경우 JSON 파싱 시도
        return ParseJson<T>(response);
    }

    /// <summary>
    /// 사용자 질문 파싱
    /// </summary>
    private T? ParseUserQuestion<T>(string response) where T : class
    {
        _logger.LogInformation("Detected user question in response");
        
        // 사용자 질문 응답을 위한 특별한 처리
        if (typeof(T) == typeof(Dictionary<string, object>))
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = "user_question",
                ["content"] = response,
                ["requires_user_input"] = true
            };
            return result as T;
        }

        return ParseJson<T>(response);
    }

    /// <summary>
    /// 오류 응답 파싱
    /// </summary>
    private T? ParseError<T>(string response) where T : class
    {
        _logger.LogWarning("Detected error pattern in response");
        
        if (typeof(T) == typeof(Dictionary<string, object>))
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = "error",
                ["content"] = response,
                ["is_error"] = true
            };
            return result as T;
        }

        return ParseJson<T>(response);
    }

    /// <summary>
    /// 완료 응답 파싱
    /// </summary>
    private T? ParseCompletion<T>(string response) where T : class
    {
        _logger.LogInformation("Detected completion response");
        
        if (typeof(T) == typeof(Dictionary<string, object>))
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = "completion",
                ["content"] = response,
                ["is_completed"] = true
            };
            return result as T;
        }

        return ParseJson<T>(response);
    }

    /// <summary>
    /// 특수 기능 파싱
    /// </summary>
    private T? ParseSpecialFunction<T>(string response) where T : class
    {
        _logger.LogInformation("Detected special function call in response");
        
        if (typeof(T) == typeof(Dictionary<string, object>))
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = "special_function",
                ["content"] = response,
                ["requires_special_handling"] = true
            };
            return result as T;
        }

        return ParseJson<T>(response);
    }
}