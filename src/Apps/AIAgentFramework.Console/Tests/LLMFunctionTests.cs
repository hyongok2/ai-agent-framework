using System.Text;
using System.Text.Json;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.LLM.Services.ToolSelection;
using AIAgentFramework.LLM.Services.Planning;
using AIAgentFramework.LLM.Services.ParameterGeneration;
using AIAgentFramework.LLM.Services.Evaluation;
using AIAgentFramework.LLM.Services.Summarization;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Console.Tests;

public static class LLMFunctionTests
{
    public static async Task TestToolSelector(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - ToolSelectorFunction 테스트 ===\n");

        var toolSelectorFunction = new ToolSelectorFunction(
            promptRegistry,
            ollama,
            toolRegistry
        );

        var context = new LLMContext
        {
            UserInput = "c:\\test-data\\sample.txt 파일을 읽어줘"
        };

        System.Console.WriteLine($"사용자 요청: {context.UserInput}\n");
        System.Console.WriteLine("--- ToolSelectorFunction 실행 중... ---\n");

        var llmResult = await toolSelectorFunction.ExecuteAsync(context);
        var toolSelection = (ToolSelectionResult)llmResult.ParsedData!;

        System.Console.WriteLine($"선택된 Tool: {toolSelection.ToolName}");
        System.Console.WriteLine($"파라미터: {toolSelection.Parameters}");
        System.Console.WriteLine($"LLM Role: {llmResult.Role}");
        System.Console.WriteLine($"원본 응답:\n{llmResult.RawResponse}\n");

        System.Console.WriteLine("=== ToolSelectorFunction 테스트 완료 ===");
    }

    public static async Task TestStreaming(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - Streaming 테스트 ===\n");

        var streamingOptions = new LLMFunctionOptions
        {
            ModelName = "gpt-oss:20b",
            EnableStreaming = true,
            TimeoutMs = 60000
        };

        var streamingToolSelector = new ToolSelectorFunction(
            promptRegistry,
            ollama,
            toolRegistry,
            streamingOptions
        );

        System.Console.WriteLine($"스트리밍 지원: {streamingToolSelector.SupportsStreaming}");
        System.Console.WriteLine($"모델: {streamingOptions.ModelName}\n");

        var streamingContext = new LLMContext
        {
            UserInput = "안녕이라고 메시지 출력해줘"
        };

        System.Console.WriteLine($"사용자 요청: {streamingContext.UserInput}\n");
        System.Console.WriteLine("--- 스트리밍 응답 수신 중... ---");
        System.Console.Write("응답: ");

        var fullResponse = new StringBuilder();
        await foreach (var chunk in streamingToolSelector.ExecuteStreamAsync(streamingContext))
        {
            if (!chunk.IsFinal && !string.IsNullOrEmpty(chunk.Content))
            {
                System.Console.Write(chunk.Content);
                fullResponse.Append(chunk.Content);
            }
            else if (chunk.IsFinal)
            {
                System.Console.WriteLine($"\n\n누적 토큰: {chunk.AccumulatedTokens}");
                System.Console.WriteLine($"총 청크 수: {chunk.Index}");
            }
        }

        System.Console.WriteLine("\n\n=== Streaming 테스트 완료 ===");
    }

    public static async Task TestTaskPlanner(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, ILLMRegistry llmRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - TaskPlanner 테스트 ===\n");

        var taskPlanner = new TaskPlannerFunction(
            promptRegistry,
            ollama,
            toolRegistry,
            llmRegistry
        );

        var planningContext = new LLMContext
        {
            UserInput = "c:\\test-data 폴더의 모든 txt 파일을 읽고, 각 파일의 내용을 요약한 다음, 결과를 summary.md 파일로 저장해줘"
        };

        System.Console.WriteLine($"사용자 요청: {planningContext.UserInput}\n");
        System.Console.WriteLine("--- TaskPlanner 실행 중... ---\n");

        var planResult = await taskPlanner.ExecuteAsync(planningContext);
        var plan = (PlanningResult)planResult.ParsedData!;

        System.Console.WriteLine($"📋 계획 요약: {plan.Summary}\n");
        System.Console.WriteLine($"✅ 실행 가능: {plan.IsExecutable}");
        System.Console.WriteLine($"⏱️  예상 시간: {plan.TotalEstimatedSeconds}초\n");

        if (plan.Steps.Count > 0)
        {
            System.Console.WriteLine("📝 실행 단계:");
            foreach (var step in plan.Steps)
            {
                System.Console.WriteLine($"\n  [{step.StepNumber}] {step.Description}");
                System.Console.WriteLine($"      Tool: {step.ToolName}");
                System.Console.WriteLine($"      Parameters: {step.Parameters}");
                if (!string.IsNullOrEmpty(step.OutputVariable))
                {
                    System.Console.WriteLine($"      Output → {step.OutputVariable}");
                }
                if (step.DependsOn.Count > 0)
                {
                    System.Console.WriteLine($"      Depends on: {string.Join(", ", step.DependsOn)}");
                }
                if (step.EstimatedSeconds.HasValue)
                {
                    System.Console.WriteLine($"      Est. time: {step.EstimatedSeconds}초");
                }
            }
        }

        if (plan.Constraints.Count > 0)
        {
            System.Console.WriteLine($"\n⚠️  제약사항:");
            foreach (var constraint in plan.Constraints)
            {
                System.Console.WriteLine($"  - {constraint}");
            }
        }

        if (!plan.IsExecutable && !string.IsNullOrEmpty(plan.ExecutionBlocker))
        {
            System.Console.WriteLine($"\n❌ 실행 불가 이유:\n{plan.ExecutionBlocker}");
        }

        System.Console.WriteLine($"\n\n원본 LLM 응답:\n{planResult.RawResponse}\n");

        System.Console.WriteLine("=== TaskPlanner 테스트 완료 ===");
    }

    public static async Task TestParameterGenerator(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - ParameterGenerator 테스트 ===\n");

        var paramGenerator = new ParameterGeneratorFunction(
            promptRegistry,
            ollama
        );

        // 시나리오 1: DirectoryReader 파라미터 생성
        System.Console.WriteLine("--- 시나리오 1: DirectoryReader 파라미터 생성 ---\n");

        var tool = toolRegistry.GetTool("DirectoryReader")!;
        var parameters1 = new Dictionary<string, object>
        {
            ["TOOL_NAME"] = tool.Metadata.Name,
            ["TOOL_INPUT_SCHEMA"] = tool.Contract.InputSchema,
            ["STEP_DESCRIPTION"] = "c:\\test-data 디렉토리에서 txt 파일 목록 조회"
        };

        var context1 = new LLMContext
        {
            UserInput = "c:\\test-data 폴더의 모든 txt 파일 목록을 보여줘",
            Parameters = parameters1
        };

        System.Console.WriteLine($"사용자 요청: {context1.UserInput}");
        System.Console.WriteLine($"Tool: {tool.Metadata.Name}");
        System.Console.WriteLine($"Step: {context1.Get<string>("STEP_DESCRIPTION")}\n");
        System.Console.WriteLine("--- ParameterGenerator 실행 중... ---\n");

        var result1 = await paramGenerator.ExecuteAsync(context1);
        var paramResult1 = (ParameterGenerationResult)result1.ParsedData!;

        System.Console.WriteLine($"✅ Valid: {paramResult1.IsValid}");
        System.Console.WriteLine($"🔧 Tool: {paramResult1.ToolName}");
        System.Console.WriteLine($"📝 Parameters: {paramResult1.Parameters}");
        if (!string.IsNullOrEmpty(paramResult1.Reasoning))
        {
            System.Console.WriteLine($"💡 Reasoning: {paramResult1.Reasoning}");
        }
        if (!string.IsNullOrEmpty(paramResult1.ErrorMessage))
        {
            System.Console.WriteLine($"❌ Error: {paramResult1.ErrorMessage}");
        }

        // 시나리오 2: FileWriter 파라미터 생성 (이전 결과 활용)
        System.Console.WriteLine("\n\n--- 시나리오 2: FileWriter 파라미터 생성 (이전 결과 활용) ---\n");

        var fileWriterTool = toolRegistry.GetTool("FileWriter")!;

        // 이전 단계 결과 시뮬레이션
        var previousResults = JsonSerializer.Serialize(new
        {
            SummaryText = "총 5개의 파일을 분석했습니다. 주요 내용은 AI 에이전트 프레임워크에 관한 것입니다.",
            FileCount = 5
        });

        var parameters2 = new Dictionary<string, object>
        {
            ["TOOL_NAME"] = fileWriterTool.Metadata.Name,
            ["TOOL_INPUT_SCHEMA"] = fileWriterTool.Contract.InputSchema,
            ["STEP_DESCRIPTION"] = "요약 결과를 summary.txt 파일로 저장",
            ["PREVIOUS_RESULTS"] = previousResults
        };

        var context2 = new LLMContext
        {
            UserInput = "결과를 summary.txt 파일로 저장해줘",
            Parameters = parameters2
        };

        System.Console.WriteLine($"사용자 요청: {context2.UserInput}");
        System.Console.WriteLine($"Tool: {fileWriterTool.Metadata.Name}");
        System.Console.WriteLine($"Step: {context2.Get<string>("STEP_DESCRIPTION")}");
        System.Console.WriteLine($"Previous Results: {previousResults}\n");
        System.Console.WriteLine("--- ParameterGenerator 실행 중... ---\n");

        var result2 = await paramGenerator.ExecuteAsync(context2);
        var paramResult2 = (ParameterGenerationResult)result2.ParsedData!;

        System.Console.WriteLine($"✅ Valid: {paramResult2.IsValid}");
        System.Console.WriteLine($"🔧 Tool: {paramResult2.ToolName}");
        System.Console.WriteLine($"📝 Parameters: {paramResult2.Parameters}");
        if (!string.IsNullOrEmpty(paramResult2.Reasoning))
        {
            System.Console.WriteLine($"💡 Reasoning: {paramResult2.Reasoning}");
        }
        if (!string.IsNullOrEmpty(paramResult2.ErrorMessage))
        {
            System.Console.WriteLine($"❌ Error: {paramResult2.ErrorMessage}");
        }

        System.Console.WriteLine("\n\n=== ParameterGenerator 테스트 완료 ===");
    }

    public static async Task TestEvaluator(IPromptRegistry promptRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - Evaluator 테스트 ===\n");

        var evaluator = new EvaluatorFunction(
            promptRegistry,
            ollama
        );

        // 시나리오 1: 성공 케이스 - 파일 읽기 성공
        System.Console.WriteLine("--- 시나리오 1: 파일 읽기 성공 평가 ---\n");

        var parameters1 = new Dictionary<string, object>
        {
            ["TASK_DESCRIPTION"] = "c:\\test-data\\report.txt 파일 읽기",
            ["EXECUTION_RESULT"] = "파일 내용: 2024년 Q4 판매 실적 보고서입니다. 총 매출 $10M을 달성했습니다.",
            ["EXPECTED_OUTCOME"] = "파일이 성공적으로 읽혀야 함",
            ["EVALUATION_CRITERIA"] = "파일 내용이 완전히 읽혔는지 확인"
        };

        var context1 = new LLMContext
        {
            UserInput = "파일 읽기 결과 평가",
            Parameters = parameters1
        };

        System.Console.WriteLine($"Task: {parameters1["TASK_DESCRIPTION"]}");
        System.Console.WriteLine($"Result: {parameters1["EXECUTION_RESULT"]}\n");
        System.Console.WriteLine("--- Evaluator 실행 중... ---\n");

        var result1 = await evaluator.ExecuteAsync(context1);
        var evalResult1 = (EvaluationResult)result1.ParsedData!;

        System.Console.WriteLine($"✅ Success: {evalResult1.IsSuccess}");
        System.Console.WriteLine($"📊 Quality Score: {evalResult1.QualityScore:F2}");
        System.Console.WriteLine($"📝 Assessment: {evalResult1.Assessment}");
        System.Console.WriteLine($"✓ Meets Criteria: {evalResult1.MeetsCriteria}");

        if (evalResult1.Strengths.Count > 0)
        {
            System.Console.WriteLine($"\n강점:");
            foreach (var strength in evalResult1.Strengths)
            {
                System.Console.WriteLine($"  + {strength}");
            }
        }

        if (evalResult1.Weaknesses.Count > 0)
        {
            System.Console.WriteLine($"\n약점:");
            foreach (var weakness in evalResult1.Weaknesses)
            {
                System.Console.WriteLine($"  - {weakness}");
            }
        }

        // 시나리오 2: 실패 케이스 - 불완전한 실행
        System.Console.WriteLine("\n\n--- 시나리오 2: 불완전한 요약 평가 ---\n");

        var parameters2 = new Dictionary<string, object>
        {
            ["TASK_DESCRIPTION"] = "5개의 txt 파일 요약",
            ["EXECUTION_RESULT"] = "3개 파일 요약 완료: file1.txt, file2.txt, file3.txt",
            ["EXPECTED_OUTCOME"] = "5개 파일 모두 요약되어야 함"
        };

        var context2 = new LLMContext
        {
            UserInput = "요약 작업 결과 평가",
            Parameters = parameters2
        };

        System.Console.WriteLine($"Task: {parameters2["TASK_DESCRIPTION"]}");
        System.Console.WriteLine($"Result: {parameters2["EXECUTION_RESULT"]}\n");
        System.Console.WriteLine("--- Evaluator 실행 중... ---\n");

        var result2 = await evaluator.ExecuteAsync(context2);
        var evalResult2 = (EvaluationResult)result2.ParsedData!;

        System.Console.WriteLine($"✅ Success: {evalResult2.IsSuccess}");
        System.Console.WriteLine($"📊 Quality Score: {evalResult2.QualityScore:F2}");
        System.Console.WriteLine($"📝 Assessment: {evalResult2.Assessment}");
        System.Console.WriteLine($"✓ Meets Criteria: {evalResult2.MeetsCriteria}");

        if (evalResult2.Recommendations.Count > 0)
        {
            System.Console.WriteLine($"\n권장사항:");
            foreach (var recommendation in evalResult2.Recommendations)
            {
                System.Console.WriteLine($"  → {recommendation}");
            }
        }

        System.Console.WriteLine("\n\n=== Evaluator 테스트 완료 ===");
    }

    public static async Task TestSummarizer(IPromptRegistry promptRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - Summarizer 테스트 ===\n");

        var summarizer = new SummarizerFunction(
            promptRegistry,
            ollama
        );

        // 시나리오 1: Brief Summary
        System.Console.WriteLine("--- 시나리오 1: Brief Summary (간단 요약) ---\n");

        var sampleText = @"AI Agent Framework는 대규모 언어 모델(LLM)과 다양한 도구를 결합하여 복잡한 작업을 자동화하는 프레임워크입니다.
이 프레임워크는 계획 수립, 도구 선택, 파라미터 생성, 실행, 평가의 5단계 워크플로우를 따릅니다.
사용자는 자연어로 요청을 입력하면, TaskPlanner가 실행 계획을 수립하고, ToolSelector가 적절한 도구를 선택합니다.
ParameterGenerator가 도구 실행에 필요한 정확한 파라미터를 생성하고, Executor가 실제로 도구를 실행합니다.
마지막으로 Evaluator가 실행 결과를 평가하여 성공 여부를 판단합니다.
프레임워크는 .NET 8 기반으로 구축되었으며, 올라마(Ollama)를 통해 로컬 LLM을 사용할 수 있습니다.";

        var parameters1 = new Dictionary<string, object>
        {
            ["CONTENT"] = sampleText,
            ["SUMMARY_STYLE"] = SummaryStyle.Brief
        };

        var context1 = new LLMContext
        {
            UserInput = "AI Agent Framework 설명 요약",
            Parameters = parameters1
        };

        System.Console.WriteLine($"원본 텍스트 길이: {sampleText.Length} 문자\n");
        System.Console.WriteLine("--- Summarizer 실행 중 (Brief) ---\n");

        var result1 = await summarizer.ExecuteAsync(context1);
        var summary1 = (SummarizationResult)result1.ParsedData!;

        System.Console.WriteLine($"📝 Summary: {summary1.Summary}");
        System.Console.WriteLine($"🎨 Style: {summary1.Style}");
        System.Console.WriteLine($"📊 Word Count: {summary1.WordCount}");
        System.Console.WriteLine($"📄 Original Length: {summary1.OriginalLength}");

        if (summary1.KeyPoints.Count > 0)
        {
            System.Console.WriteLine($"\n핵심 포인트:");
            foreach (var point in summary1.KeyPoints)
            {
                System.Console.WriteLine($"  • {point}");
            }
        }

        // 시나리오 2: Standard Summary
        System.Console.WriteLine("\n\n--- 시나리오 2: Standard Summary (표준 요약) ---\n");

        var parameters2 = new Dictionary<string, object>
        {
            ["CONTENT"] = sampleText,
            ["SUMMARY_STYLE"] = SummaryStyle.Standard
        };

        var context2 = new LLMContext
        {
            UserInput = "AI Agent Framework 설명 요약",
            Parameters = parameters2
        };

        System.Console.WriteLine("--- Summarizer 실행 중 (Standard) ---\n");

        var result2 = await summarizer.ExecuteAsync(context2);
        var summary2 = (SummarizationResult)result2.ParsedData!;

        System.Console.WriteLine($"📝 Summary: {summary2.Summary}");
        System.Console.WriteLine($"🎨 Style: {summary2.Style}");
        System.Console.WriteLine($"📊 Word Count: {summary2.WordCount}");

        if (summary2.KeyPoints.Count > 0)
        {
            System.Console.WriteLine($"\n핵심 포인트:");
            foreach (var point in summary2.KeyPoints)
            {
                System.Console.WriteLine($"  • {point}");
            }
        }

        System.Console.WriteLine("\n\n=== Summarizer 테스트 완료 ===");
    }
}
