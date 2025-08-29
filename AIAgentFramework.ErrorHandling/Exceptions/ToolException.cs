namespace AIAgentFramework.ErrorHandling.Exceptions;

public class ToolException : Exception
{
    public string? ToolName { get; }
    public string? ToolType { get; }

    public ToolException(string message) : base(message) { }
    
    public ToolException(string message, Exception innerException) : base(message, innerException) { }
    
    public ToolException(string message, string? toolName, string? toolType) : base(message)
    {
        ToolName = toolName;
        ToolType = toolType;
    }
}