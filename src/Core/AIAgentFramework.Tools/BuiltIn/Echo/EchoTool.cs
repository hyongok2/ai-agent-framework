using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Tools.BuiltIn.Echo;

/// <summary>
/// 가장 간단한 Tool - 입력을 그대로 반환
/// 테스트 및 검증용
/// </summary>
public class EchoTool : ITool
{
    public IToolMetadata Metadata { get; }
    public IToolContract Contract { get; }

    public EchoTool()
    {
        Metadata = new ToolMetadata(
            name: "Echo",
            description: "입력받은 메시지를 그대로 반환합니다.",
            type: ToolType.BuiltIn
        );

        Contract = new ToolContract(requiresParameters: true);
    }

    public Task<IToolResult> ExecuteAsync(
        object? input,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;

        // 입력 검증
        if (!Contract.ValidateInput(input))
        {
            return Task.FromResult<IToolResult>(
                ToolResult.Failure(
                    Metadata.Name,
                    "입력이 필요합니다.",
                    startedAt
                )
            );
        }

        // 입력을 그대로 반환
        var result = ToolResult.Success(
            Metadata.Name,
            data: input,
            startedAt
        );

        return Task.FromResult<IToolResult>(result);
    }
}
