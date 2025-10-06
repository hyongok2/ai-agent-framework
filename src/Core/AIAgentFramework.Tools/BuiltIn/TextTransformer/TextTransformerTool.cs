using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Tools.BuiltIn.TextTransformer;

/// <summary>
/// 텍스트 변환 도구
/// 텍스트를 대문자, 소문자, 타이틀케이스 등으로 변환합니다
/// </summary>
public class TextTransformerTool : ITool
{
    public IToolMetadata Metadata { get; }
    public IToolContract Contract { get; }

    public TextTransformerTool()
    {
        Metadata = new ToolMetadata(
            name: "TextTransformer",
            description: "텍스트를 대문자, 소문자, 타이틀케이스 등으로 변환합니다.",
            type: ToolType.BuiltIn
        );

        Contract = new ToolContract(
            requiresParameters: true,
            inputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "text": {
                            "type": "string",
                            "description": "변환할 텍스트"
                        },
                        "mode": {
                            "type": "string",
                            "enum": ["upper", "lower", "title"],
                            "description": "변환 모드 (upper: 대문자, lower: 소문자, title: 타이틀케이스)"
                        }
                    },
                    "required": ["text", "mode"]
                }
                """,
            outputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "OriginalText": { "type": "string", "description": "원본 텍스트" },
                        "TransformedText": { "type": "string", "description": "변환된 텍스트" },
                        "Mode": { "type": "string", "description": "사용된 변환 모드" },
                        "Length": { "type": "integer", "description": "텍스트 길이" }
                    },
                    "required": ["OriginalText", "TransformedText", "Mode", "Length"]
                }
                """
        );
    }

    public Task<IToolResult> ExecuteAsync(
        object? input,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;

        var validationResult = ValidateAndExtractInput(input, startedAt);
        if (!validationResult.IsValid)
        {
            return Task.FromResult(validationResult.ErrorResult!);
        }

        try
        {
            var transformed = Transform(validationResult.Text!, validationResult.Mode!);
            var result = new TextTransformResult
            {
                OriginalText = validationResult.Text!,
                TransformedText = transformed,
                Mode = validationResult.Mode!,
                Length = transformed.Length
            };

            return Task.FromResult<IToolResult>(
                ToolResult.Success(Metadata.Name, result, startedAt)
            );
        }
        catch (Exception ex)
        {
            return Task.FromResult<IToolResult>(
                ToolResult.Failure(Metadata.Name, $"변환 실패: {ex.Message}", startedAt)
            );
        }
    }

    private (bool IsValid, string? Text, string? Mode, IToolResult? ErrorResult) ValidateAndExtractInput(
        object? input,
        DateTimeOffset startedAt)
    {
        if (!Contract.ValidateInput(input))
        {
            return (false, null, null, ToolResult.Failure(Metadata.Name, "텍스트와 변환 모드가 필요합니다.", startedAt));
        }

        var (text, mode) = ExtractInput(input);

        if (string.IsNullOrWhiteSpace(text))
        {
            return (false, null, null, ToolResult.Failure(Metadata.Name, "텍스트가 비어있습니다.", startedAt));
        }

        if (string.IsNullOrWhiteSpace(mode) || !IsValidMode(mode))
        {
            return (false, text, null, ToolResult.Failure(Metadata.Name, "올바른 변환 모드가 아닙니다. (upper, lower, title 중 선택)", startedAt));
        }

        return (true, text, mode, null);
    }

    private (string? Text, string? Mode) ExtractInput(object? input)
    {
        if (input is Dictionary<string, object> dict)
        {
            var text = dict.ContainsKey("text") ? dict["text"]?.ToString() : null;
            var mode = dict.ContainsKey("mode") ? dict["mode"]?.ToString()?.ToLower() : null;
            return (text, mode);
        }

        return (null, null);
    }

    private bool IsValidMode(string mode)
    {
        return mode.ToLower() is "upper" or "lower" or "title";
    }

    private string Transform(string text, string mode)
    {
        return mode.ToLower() switch
        {
            "upper" => text.ToUpper(),
            "lower" => text.ToLower(),
            "title" => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower()),
            _ => text
        };
    }
}

public class TextTransformResult
{
    public required string OriginalText { get; init; }
    public required string TransformedText { get; init; }
    public required string Mode { get; init; }
    public required int Length { get; init; }
}
