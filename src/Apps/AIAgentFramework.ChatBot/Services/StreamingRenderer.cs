using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.ChatBot.Services;

/// <summary>
/// 스트리밍 출력 렌더러
/// </summary>
public class StreamingRenderer
{
    private readonly object _lock = new();

    public void RenderChunk(IStreamChunk chunk)
    {
        lock (_lock)
        {
            switch (chunk.Type)
            {
                case StreamChunkType.Status:
                    RenderStatus(chunk.Content);
                    break;

                case StreamChunkType.Text:
                    RenderText(chunk.Content);
                    break;

                case StreamChunkType.Error:
                    RenderError(chunk.Content);
                    break;

                case StreamChunkType.Complete:
                    RenderComplete(chunk.Content);
                    break;
            }
        }
    }

    private void RenderStatus(string content)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(content);
        Console.ResetColor();
    }

    private void RenderText(string content)
    {
        Console.Write(content);
    }

    public void RenderError(string content)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n❌ Error: {content}");
        Console.ResetColor();
    }

    private void RenderComplete(string? content)
    {
        if (!string.IsNullOrEmpty(content))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ {content}");
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    public void RenderUserMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n👤 You: {message}");
        Console.ResetColor();
    }

    public void RenderAssistantStart()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("\n🤖 Assistant: ");
        Console.ResetColor();
    }

    public void RenderSystemMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[{message}]");
        Console.ResetColor();
    }
}
