using System;

namespace Agent.Abstractions.Core.Common.Exceptions;

/// <summary>
/// 도구 실행 실패 예외
/// </summary>
public class ToolExecutionException : AgentException
{
    public string ToolName { get; }
    
    public ToolExecutionException(string toolName, string message, Exception? innerException = null)
        : base($"Tool execution failed for {toolName}: {message}", innerException!, "TOOL_EXECUTION_FAILED")
    {
        ToolName = toolName;
    }
}