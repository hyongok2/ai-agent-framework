using System.Text.Json;
using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Core.Services;

/// <summary>
/// 파일 기반 로거
/// 각 실행을 개별 JSON 파일로 저장하여 분석 용이성 제공
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _baseLogPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private static int _llmSequence = 0;
    private static int _toolSequence = 0;
    private static readonly object _lock = new object();

    public FileLogger(string baseLogPath = "logs")
    {
        _baseLogPath = baseLogPath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 한글 깨짐 방지
        };
    }

    public async Task LogAsync(ILogEntry logEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GenerateLogFilePath(logEntry);
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(logEntry, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }
        catch
        {
            // 로깅 실패는 조용히 무시 (애플리케이션 실행에 영향 없도록)
        }
    }

    private string GenerateLogFilePath(ILogEntry logEntry)
    {
        var date = logEntry.Timestamp.ToString("yyyy-MM-dd");
        var time = logEntry.Timestamp.ToString("HHmmss");

        int sequence;
        lock (_lock)
        {
            sequence = logEntry.LogType.ToLower() == "llm"
                ? ++_llmSequence
                : ++_toolSequence;
        }

        // LLM과 Tool 모두 같은 폴더에 저장 (시간순 정렬 분석 용이)
        // 파일명: 날짜_시간_타입_대상이름_순서.json
        var logType = logEntry.LogType.ToUpper(); // "LLM" 또는 "TOOL"
        var fileName = $"{date}_{time}_{logType}_{logEntry.TargetName}_{sequence:D3}.json";

        return Path.Combine(_baseLogPath, fileName);
    }
}
