namespace AIAgentFramework.ErrorHandling.Exceptions;

public class LLMException : Exception
{
    public string? ModelName { get; }
    public string? FunctionName { get; }

    public LLMException(string message) : base(message) { }
    
    public LLMException(string message, Exception innerException) : base(message, innerException) { }
    
    public LLMException(string message, string? modelName, string? functionName) : base(message)
    {
        ModelName = modelName;
        FunctionName = functionName;
    }
}