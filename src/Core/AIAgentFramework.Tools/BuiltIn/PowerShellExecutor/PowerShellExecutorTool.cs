using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Tools.BuiltIn.PowerShellExecutor;

/// <summary>
/// PowerShell 명령 실행 Tool
/// </summary>
public class PowerShellExecutorTool : ITool
{
    public IToolMetadata Metadata { get; }
    public IToolContract Contract { get; }

    public PowerShellExecutorTool()
    {
        Metadata = new ToolMetadata(
            name: "PowerShellExecutor",
            description: "PowerShell 명령 또는 스크립트를 실행하고 결과를 반환합니다.",
            type: ToolType.BuiltIn
        );

        Contract = new ToolContract(
            requiresParameters: true,
            inputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "command": {
                            "type": "string",
                            "description": "실행할 PowerShell 명령 또는 스크립트"
                        },
                        "workingDirectory": {
                            "type": "string",
                            "description": "작업 디렉토리 경로 (선택적)"
                        },
                        "timeoutSeconds": {
                            "type": "integer",
                            "description": "타임아웃 시간(초), 기본값: 30",
                            "default": 30
                        }
                    },
                    "required": ["command"]
                }
                """,
            outputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "Output": {
                            "type": "string",
                            "description": "명령 실행 표준 출력"
                        },
                        "Error": {
                            "type": "string",
                            "description": "명령 실행 오류 출력"
                        },
                        "ExitCode": {
                            "type": "integer",
                            "description": "프로세스 종료 코드"
                        },
                        "ExecutionTimeMs": {
                            "type": "integer",
                            "description": "실행 시간(밀리초)"
                        }
                    }
                }
                """
        );
    }

    public async Task<IToolResult> ExecuteAsync(
        object? input,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 입력 검증
            if (!Contract.ValidateInput(input))
            {
                return ToolResult.Failure(Metadata.Name, "입력이 필요합니다.", startTime);
            }

            // Dictionary에서 파라미터 추출
            var inputDict = input as Dictionary<string, object>;
            if (inputDict == null)
            {
                return ToolResult.Failure(Metadata.Name, "입력 형식이 올바르지 않습니다.", startTime);
            }

            if (!inputDict.TryGetValue("command", out var commandObj) || commandObj is not string command)
            {
                return ToolResult.Failure(Metadata.Name, "command 파라미터가 필요합니다.", startTime);
            }

            var workingDir = inputDict.TryGetValue("workingDirectory", out var wdObj) && wdObj is string wd
                ? wd
                : Environment.CurrentDirectory;

            var timeoutSeconds = inputDict.TryGetValue("timeoutSeconds", out var timeoutObj) && timeoutObj is int timeout
                ? timeout
                : 30;

            // 작업 디렉토리 검증
            if (!Directory.Exists(workingDir))
            {
                return ToolResult.Failure(Metadata.Name, $"작업 디렉토리가 존재하지 않습니다: {workingDir}", startTime);
            }

            // PowerShell 프로세스 설정
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{EscapeCommand(command)}\"",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 타임아웃 처리
            var completed = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000), cancellationToken);

            if (!completed)
            {
                try
                {
                    process.Kill();
                }
                catch { }

                return ToolResult.Failure(Metadata.Name, $"명령 실행 시간 초과 ({timeoutSeconds}초)", startTime);
            }

            sw.Stop();

            var output = outputBuilder.ToString().TrimEnd();
            var error = errorBuilder.ToString().TrimEnd();
            var exitCode = process.ExitCode;

            // 결과 생성
            var result = new
            {
                Output = output,
                Error = error,
                ExitCode = exitCode,
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };

            var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            // 종료 코드가 0이 아니면 경고로 처리
            var isSuccess = exitCode == 0;

            if (isSuccess)
            {
                return ToolResult.Success(Metadata.Name, result, startTime);
            }
            else
            {
                return ToolResult.Failure(Metadata.Name, error, startTime);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ToolResult.Failure(Metadata.Name, $"PowerShell 실행 중 오류 발생: {ex.Message}", startTime);
        }
    }

    /// <summary>
    /// PowerShell 명령에서 특수문자 이스케이프
    /// </summary>
    private static string EscapeCommand(string command)
    {
        // 큰따옴표 이스케이프
        return command.Replace("\"", "`\"");
    }
}
