using System.Text;
using System.Text.Json;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Execution.Models;
using AIAgentFramework.Execution.Services;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.LLM.Services.Evaluation;
using AIAgentFramework.LLM.Services.ParameterGeneration;
using AIAgentFramework.LLM.Services.Planning;
using AIAgentFramework.LLM.Services.Summarization;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Console.Tests;

public static class LLMFunctionTests
{
    public static async Task TestTaskPlanner(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, ILLMRegistry llmRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - TaskPlanner 테스트 ===\n");

        // 스트리밍 활성화
        var taskPlannerOptions = new LLMFunctionOptions
        {
            EnableStreaming = true,
            ModelName = "gpt-oss:20b"
        };

        var taskPlanner = new TaskPlannerFunction(
            promptRegistry,
            ollama,
            toolRegistry,
            llmRegistry,
            taskPlannerOptions
        );

        var planningContext = new LLMContext
        {
            UserInput = "c:\\test-data 폴더의 모든 txt 파일을 읽고, 각 파일의 내용을 요약한 다음, 결과를 summary.md 파일로 저장해줘"
        };

        System.Console.WriteLine($"사용자 요청: {planningContext.UserInput}\n");
        System.Console.WriteLine("--- TaskPlanner 실행 중... ---\n");

        // 스트리밍으로 실행
        PlanningResult? plan = null;
        await foreach (var chunk in taskPlanner.ExecuteStreamAsync(planningContext))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                System.Console.Write(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                plan = (PlanningResult)chunk.ParsedResult;
            }
        }
        System.Console.WriteLine("\n");

        if (plan != null)
        {
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
        }
        else
        {
            System.Console.WriteLine($"\n❌ 실패: 결과를 파싱할 수 없습니다");
        }

        System.Console.WriteLine("\n=== TaskPlanner 테스트 완료 ===");
    }

    public static async Task TestParameterGenerator(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - ParameterGenerator 테스트 ===\n");

        // 스트리밍 활성화
        var paramGeneratorOptions = new LLMFunctionOptions
        {
            EnableStreaming = true,
            ModelName = "gpt-oss:20b"
        };

        var paramGenerator = new ParameterGeneratorFunction(
            promptRegistry,
            ollama,
            paramGeneratorOptions
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

        // 스트리밍으로 실행
        ParameterGenerationResult? paramResult1 = null;
        await foreach (var chunk in paramGenerator.ExecuteStreamAsync(context1))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                System.Console.Write(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                paramResult1 = (ParameterGenerationResult)chunk.ParsedResult;
            }
        }
        System.Console.WriteLine("\n");

        if (paramResult1 != null)
        {
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

        // 스트리밍으로 실행
        ParameterGenerationResult? paramResult2 = null;
        await foreach (var chunk in paramGenerator.ExecuteStreamAsync(context2))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                System.Console.Write(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                paramResult2 = (ParameterGenerationResult)chunk.ParsedResult;
            }
        }
        System.Console.WriteLine("\n");

        if (paramResult2 != null)
        {
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
        }

        System.Console.WriteLine("\n\n=== ParameterGenerator 테스트 완료 ===");
    }

    public static async Task TestEvaluator(IPromptRegistry promptRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - Evaluator 테스트 ===\n");

        // 스트리밍 활성화
        var evaluatorOptions = new LLMFunctionOptions
        {
            EnableStreaming = true,
            ModelName = "gpt-oss:20b"
        };

        var evaluator = new EvaluatorFunction(
            promptRegistry,
            ollama,
            evaluatorOptions
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

        // 스트리밍으로 실행
        EvaluationResult? evalResult1 = null;
        await foreach (var chunk in evaluator.ExecuteStreamAsync(context1))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                System.Console.Write(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                evalResult1 = (EvaluationResult)chunk.ParsedResult;
            }
        }
        System.Console.WriteLine("\n");

        if (evalResult1 != null)
        {
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

        // 스트리밍으로 실행
        EvaluationResult? evalResult2 = null;
        await foreach (var chunk in evaluator.ExecuteStreamAsync(context2))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                System.Console.Write(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                evalResult2 = (EvaluationResult)chunk.ParsedResult;
            }
        }
        System.Console.WriteLine("\n");

        if (evalResult2 != null)
        {
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
        }

        System.Console.WriteLine("\n\n=== Evaluator 테스트 완료 ===");
    }

    public static async Task TestSummarizer(IPromptRegistry promptRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - Summarizer 테스트 ===\n");

        // 스트리밍 활성화
        var summarizerOptions = new LLMFunctionOptions
        {
            EnableStreaming = true,
            ModelName = "gpt-oss:20b"
        };

        var summarizer = new SummarizerFunction(
            promptRegistry,
            ollama,
            summarizerOptions
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

        // 스트리밍으로 실행
        SummarizationResult? summary1 = null;
        await foreach (var chunk in summarizer.ExecuteStreamAsync(context1))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                System.Console.Write(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                summary1 = (SummarizationResult)chunk.ParsedResult;
            }
        }
        System.Console.WriteLine("\n");

        System.Console.WriteLine($"📝 Summary: {summary1!.Summary}");
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

        // 스트리밍으로 실행
        SummarizationResult? summary2 = null;
        await foreach (var chunk in summarizer.ExecuteStreamAsync(context2))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                System.Console.Write(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                summary2 = (SummarizationResult)chunk.ParsedResult;
            }
        }
        System.Console.WriteLine("\n");

        System.Console.WriteLine($"📝 Summary: {summary2!.Summary}");
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

    public static async Task TestExecutor(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, ILLMRegistry llmRegistry, OllamaProvider ollama)
    {
        try { System.Console.Clear(); } catch { } // 비대화형 실행 시 예외 무시
        System.Console.WriteLine("=== AI Agent Framework - PlanExecutor 테스트 ===\n");

        // LLM Function 등록 (시나리오 3, 4를 위해) - 스트리밍 활성화
        var summarizerOptions = new LLMFunctionOptions
        {
            EnableStreaming = true,
            ModelName = "gpt-oss:20b"
        };
        var summarizer = new SummarizerFunction(promptRegistry, ollama, summarizerOptions);
        llmRegistry.Register(summarizer);

        // PlanExecutor 생성 - 의존성 주입
        var parameterGenerator = new ParameterGeneratorFunction(promptRegistry, ollama);

        var executableResolver = new ExecutableResolver(toolRegistry, llmRegistry);
        var parameterProcessor = new ParameterProcessor(parameterGenerator);
        var toolExecutor = new ToolStepExecutor();
        var llmExecutor = new LLMFunctionStepExecutor();

        var executor = new PlanExecutor(
            executableResolver,
            parameterProcessor,
            toolExecutor,
            llmExecutor
        );

        // 시나리오 1: 간단한 계획 실행 (파일 읽기)
        System.Console.WriteLine("--- 시나리오 1: 단일 단계 계획 실행 ---\n");

        var plan1 = new PlanningResult
        {
            Summary = "텍스트 파일 읽기",
            IsExecutable = true,
            Steps = new List<TaskStep>
            {
                new TaskStep
                {
                    StepNumber = 1,
                    Description = "sample.txt 파일 읽기",
                    ToolName = "FileReader",
                    Parameters = "c:\\\\test-data\\\\sample.txt",
                    OutputVariable = "fileContent"
                }
            },
            TotalEstimatedSeconds = 5
        };

        var input1 = new ExecutionInput
        {
            Plan = plan1,
            UserRequest = "c:\\test-data\\sample.txt 파일을 읽어줘"
        };

        System.Console.WriteLine($"계획: {plan1.Summary}");
        System.Console.WriteLine($"단계 수: {plan1.Steps.Count}");
        System.Console.WriteLine($"\n--- Executor 실행 중... ---\n");

        // AgentContext 생성
        var agentContext1 = AgentContext.Create();

        var result1 = await executor.ExecuteAsync(input1, agentContext1);

        System.Console.WriteLine($"✅ 전체 성공: {result1.IsSuccess}");
        System.Console.WriteLine($"📊 성공한 단계: {result1.SuccessfulSteps}/{result1.Steps.Count}");
        System.Console.WriteLine($"⏱️  총 실행 시간: {result1.TotalExecutionTimeMs}ms");

        if (!string.IsNullOrEmpty(result1.Summary))
        {
            System.Console.WriteLine($"📝 요약: {result1.Summary}");
        }

        System.Console.WriteLine($"\n단계별 결과:");
        foreach (var step in result1.Steps)
        {
            System.Console.WriteLine($"\n  Step {step.StepNumber}: {step.Description}");
            System.Console.WriteLine($"    Tool: {step.ToolName}");
            System.Console.WriteLine($"    Success: {step.IsSuccess}");
            System.Console.WriteLine($"    Time: {step.ExecutionTimeMs}ms");
            if (!string.IsNullOrEmpty(step.Output))
            {
                var preview = step.Output.Length > 100 ? step.Output.Substring(0, 100) + "..." : step.Output;
                System.Console.WriteLine($"    Output: {preview}");
            }
            if (!string.IsNullOrEmpty(step.ErrorMessage))
            {
                System.Console.WriteLine($"    Error: {step.ErrorMessage}");
            }
        }

        // 시나리오 2: 다단계 계획 실행 (의존성 있음)
        System.Console.WriteLine("\n\n--- 시나리오 2: 다단계 계획 실행 (파일 읽고 쓰기) ---\n");

        var plan2 = new PlanningResult
        {
            Summary = "파일 읽고 다른 파일에 쓰기",
            IsExecutable = true,
            Steps = new List<TaskStep>
            {
                new TaskStep
                {
                    StepNumber = 1,
                    Description = "sample.txt 파일 읽기",
                    ToolName = "FileReader",
                    Parameters = "c:\\\\test-data\\\\sample.txt",
                    OutputVariable = "fileContent"
                },
                new TaskStep
                {
                    StepNumber = 2,
                    Description = "읽은 내용을 output.txt에 쓰기",
                    ToolName = "FileWriter",
                    Parameters = "{\"path\":\"c:\\\\test-data\\\\output.txt\",\"content\":\"${fileContent}\"}",
                    OutputVariable = "writeResult",
                    DependsOn = new List<int> { 1 }
                }
            },
            TotalEstimatedSeconds = 10
        };

        var input2 = new ExecutionInput
        {
            Plan = plan2,
            UserRequest = "c:\\test-data\\sample.txt 파일을 읽고 output.txt에 복사해줘"
        };

        System.Console.WriteLine($"계획: {plan2.Summary}");
        System.Console.WriteLine($"단계 수: {plan2.Steps.Count}");
        System.Console.WriteLine($"\n--- Executor 실행 중... ---\n");

        // AgentContext 생성
        var agentContext2 = AgentContext.Create();

        var result2 = await executor.ExecuteAsync(input2, agentContext2);

        System.Console.WriteLine($"✅ 전체 성공: {result2.IsSuccess}");
        System.Console.WriteLine($"📊 성공한 단계: {result2.SuccessfulSteps}/{result2.Steps.Count}");
        System.Console.WriteLine($"⏱️  총 실행 시간: {result2.TotalExecutionTimeMs}ms");

        if (!string.IsNullOrEmpty(result2.ErrorMessage))
        {
            System.Console.WriteLine($"❌ 오류: {result2.ErrorMessage}");
        }

        System.Console.WriteLine($"\n단계별 결과:");
        foreach (var step in result2.Steps)
        {
            System.Console.WriteLine($"\n  Step {step.StepNumber}: {step.Description}");
            System.Console.WriteLine($"    Tool: {step.ToolName}");
            System.Console.WriteLine($"    Success: {step.IsSuccess}");
            System.Console.WriteLine($"    Time: {step.ExecutionTimeMs}ms");
            if (!string.IsNullOrEmpty(step.Parameters))
            {
                var paramPreview = step.Parameters.Length > 200 ? step.Parameters.Substring(0, 200) + "..." : step.Parameters;
                System.Console.WriteLine($"    Parameters: {paramPreview}");
            }
            if (!string.IsNullOrEmpty(step.OutputVariable))
            {
                System.Console.WriteLine($"    Variable: {step.OutputVariable}");
            }
            if (!string.IsNullOrEmpty(step.Output))
            {
                var preview = step.Output.Length > 100 ? step.Output.Substring(0, 100) + "..." : step.Output;
                System.Console.WriteLine($"    Output: {preview}");
            }
            if (!string.IsNullOrEmpty(step.ErrorMessage))
            {
                System.Console.WriteLine($"    Error: {step.ErrorMessage}");
            }
        }

        // 시나리오 3: 간단한 LLM Function 단독 실행 (Summarizer)
        System.Console.WriteLine("\n\n--- 시나리오 3: LLM Function 단독 실행 (Summarizer) ---\n");

        var plan3 = new PlanningResult
        {
            Summary = "텍스트 요약하기",
            IsExecutable = true,
            Steps = new List<TaskStep>
            {
                new TaskStep
                {
                    StepNumber = 1,
                    Description = "AI Agent Framework 설명 요약",
                    ToolName = "Summarizer",
                    Parameters = "{\"CONTENT\":\"AI Agent Framework는 .NET 8 기반의 확장 가능한 AI 에이전트 시스템입니다. LLM 기능과 Tool을 조합하여 복잡한 작업을 자동화할 수 있습니다.\"}",
                    OutputVariable = "summary"
                }
            },
            TotalEstimatedSeconds = 5
        };

        var input3 = new ExecutionInput
        {
            Plan = plan3,
            UserRequest = "AI Agent Framework 설명을 요약해줘"
        };

        System.Console.WriteLine($"계획: {plan3.Summary}");
        System.Console.WriteLine($"단계 수: {plan3.Steps.Count}");
        var agentContext3 = AgentContext.Create();

        // 스트리밍 출력 콜백
        static void OnStepCompleted3(StepExecutionResult stepResult)
        {
            System.Console.WriteLine($"\n[Step {stepResult.StepNumber} 완료] {stepResult.Description}");
            System.Console.WriteLine($"  {(stepResult.IsSuccess ? "✅ 성공" : "❌ 실패")} - {stepResult.ExecutionTimeMs}ms");

            if (!string.IsNullOrEmpty(stepResult.Output) && stepResult.Output.Length > 0)
            {
                var preview = stepResult.Output.Length > 100
                    ? stepResult.Output[..100] + "..."
                    : stepResult.Output;
                System.Console.WriteLine($"  📤 {preview}");
            }

            if (!string.IsNullOrEmpty(stepResult.ErrorMessage))
            {
                var errorPreview = stepResult.ErrorMessage.Length > 100
                    ? stepResult.ErrorMessage[..100] + "..."
                    : stepResult.ErrorMessage;
                System.Console.WriteLine($"  ❌ 오류: {errorPreview}");
            }
        }

        System.Console.WriteLine("\n실행 시작...\n");

        // LLM 스트리밍 출력 콜백
        static void OnStreamChunk(string chunk)
        {
            System.Console.Write(chunk); // 실시간 출력
        }

        var result3 = await executor.ExecuteAsync(input3, agentContext3, OnStepCompleted3, OnStreamChunk);

        System.Console.WriteLine($"\n✅ 전체 성공: {result3.IsSuccess}");
        System.Console.WriteLine($"📊 성공한 단계: {result3.SuccessfulSteps}/{result3.Steps.Count}");
        System.Console.WriteLine($"⏱️  총 실행 시간: {result3.TotalExecutionTimeMs}ms");

        if (!string.IsNullOrEmpty(result3.ErrorMessage))
        {
            System.Console.WriteLine($"❌ 오류: {result3.ErrorMessage}");
        }

        System.Console.WriteLine($"\n단계별 결과:");
        foreach (var step in result3.Steps)
        {
            System.Console.WriteLine($"\n  Step {step.StepNumber}: {step.Description}");
            System.Console.WriteLine($"    실행: {step.ToolName}");
            System.Console.WriteLine($"    Success: {step.IsSuccess}");
            System.Console.WriteLine($"    Time: {step.ExecutionTimeMs}ms");
            if (!string.IsNullOrEmpty(step.Output))
            {
                var preview = step.Output.Length > 200 ? step.Output.Substring(0, 200) + "..." : step.Output;
                System.Console.WriteLine($"    Output: {preview}");
            }
            if (!string.IsNullOrEmpty(step.ErrorMessage))
            {
                System.Console.WriteLine($"    Error: {step.ErrorMessage}");
            }
        }

        System.Console.WriteLine("\n💡 참고: Summarizer는 LLM Function으로 실행되었습니다.");

        // 시나리오 4: Tool + LLM Function 혼합 실행 (FileReader → Summarizer)
        System.Console.WriteLine("\n\n--- 시나리오 4: Tool + LLM Function 혼합 실행 (파일 읽고 요약) ---\n");

        var plan4 = new PlanningResult
        {
            Summary = "파일을 읽고 LLM으로 요약",
            IsExecutable = true,
            Steps = new List<TaskStep>
            {
                new TaskStep
                {
                    StepNumber = 1,
                    Description = "sample.txt 파일 읽기",
                    ToolName = "FileReader",
                    Parameters = "c:\\\\test-data\\\\sample.txt",
                    OutputVariable = "fileContent"
                },
                new TaskStep
                {
                    StepNumber = 2,
                    Description = "파일 내용 요약하기",
                    ToolName = "Summarizer",  // LLM Function
                    // JSON 경로 표현식 사용: ${fileContent.Content}
                    Parameters = "{\"CONTENT\":\"${fileContent.Content}\"}",
                    OutputVariable = "summary",
                    DependsOn = new List<int> { 1 }
                }
            },
            TotalEstimatedSeconds = 15
        };

        var input4 = new ExecutionInput
        {
            Plan = plan4,
            UserRequest = "c:\\test-data\\sample.txt 파일을 읽고 요약해줘"
        };

        System.Console.WriteLine($"계획: {plan4.Summary}");
        System.Console.WriteLine($"단계 수: {plan4.Steps.Count}");
        System.Console.WriteLine($"  Step 1: {plan4.Steps[0].ToolName} (Tool)");
        System.Console.WriteLine($"  Step 2: {plan4.Steps[1].ToolName} (LLM Function)");
        System.Console.WriteLine();

        var agentContext4 = AgentContext.Create();

        // 스트리밍 출력 콜백 정의
        static void OnStepCompleted(StepExecutionResult stepResult)
        {
            System.Console.WriteLine($"\n[Step {stepResult.StepNumber} 완료] {stepResult.Description}");
            System.Console.WriteLine($"  {(stepResult.IsSuccess ? "✅ 성공" : "❌ 실패")} - {stepResult.ExecutionTimeMs}ms");

            if (!string.IsNullOrEmpty(stepResult.Output) && stepResult.Output.Length > 0)
            {
                var preview = stepResult.Output.Length > 100
                    ? stepResult.Output[..100] + "..."
                    : stepResult.Output;
                System.Console.WriteLine($"  📤 {preview}");
            }

            if (!string.IsNullOrEmpty(stepResult.ErrorMessage))
            {
                var errorPreview = stepResult.ErrorMessage.Length > 100
                    ? stepResult.ErrorMessage[..100] + "..."
                    : stepResult.ErrorMessage;
                System.Console.WriteLine($"  ❌ 오류: {errorPreview}");
            }
        }

        System.Console.WriteLine("실행 시작...\n");

        // LLM 스트리밍 출력 콜백
        static void OnStreamChunk4(string chunk)
        {
            System.Console.Write(chunk);
        }

        var result4 = await executor.ExecuteAsync(input4, agentContext4, OnStepCompleted, OnStreamChunk4);

        // 최종 결과 출력
        System.Console.WriteLine($"\n{'=',-60}");
        System.Console.WriteLine($"✅ 전체 성공: {result4.IsSuccess}");
        System.Console.WriteLine($"📊 성공한 단계: {result4.SuccessfulSteps}/{result4.Steps.Count}");
        System.Console.WriteLine($"⏱️  총 실행 시간: {result4.TotalExecutionTimeMs}ms");

        if (!string.IsNullOrEmpty(result4.ErrorMessage))
        {
            System.Console.WriteLine($"❌ 오류: {result4.ErrorMessage}");
        }

        // 각 단계 상세 결과
        System.Console.WriteLine($"\n{'=',-60}");
        System.Console.WriteLine("단계별 상세 결과:");
        System.Console.WriteLine($"{'=',-60}");

        foreach (var step in result4.Steps)
        {
            System.Console.WriteLine($"\n[Step {step.StepNumber}] {step.Description}");
            System.Console.WriteLine($"  🔧 실행: {step.ToolName}");
            System.Console.WriteLine($"  {(step.IsSuccess ? "✅" : "❌")} 결과: {(step.IsSuccess ? "성공" : "실패")}");
            System.Console.WriteLine($"  ⏱️  시간: {step.ExecutionTimeMs}ms");

            if (!string.IsNullOrEmpty(step.Parameters))
            {
                var paramPreview = step.Parameters.Length > 150 ? step.Parameters.Substring(0, 150) + "..." : step.Parameters;
                System.Console.WriteLine($"  📝 파라미터: {paramPreview}");
            }

            if (!string.IsNullOrEmpty(step.OutputVariable))
            {
                System.Console.WriteLine($"  💾 변수명: {step.OutputVariable}");
            }

            if (!string.IsNullOrEmpty(step.Output))
            {
                var preview = step.Output.Length > 300 ? step.Output.Substring(0, 300) + "..." : step.Output;
                System.Console.WriteLine($"  📤 출력:\n{preview}");
            }

            if (!string.IsNullOrEmpty(step.ErrorMessage))
            {
                System.Console.WriteLine($"  ❌ 오류: {step.ErrorMessage}");
            }
        }

        System.Console.WriteLine($"\n{'=',-60}");
        System.Console.WriteLine("💡 Step 1은 Tool(FileReader), Step 2는 LLM Function(Summarizer) 실행");
        System.Console.WriteLine($"{'=',-60}");

        System.Console.WriteLine("\n\n=== PlanExecutor 테스트 완료 ===");
    }
}
