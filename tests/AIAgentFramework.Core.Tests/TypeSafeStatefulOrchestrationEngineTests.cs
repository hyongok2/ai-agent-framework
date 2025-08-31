using AIAgentFramework.Core.Actions.Factories;
using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Core.Orchestration.Execution;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.User;
using AIAgentFramework.Orchestration;
using AIAgentFramework.Orchestration.Engines;
using AIAgentFramework.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Core.Tests;

public class TypeSafeStatefulOrchestrationEngineTests
{
    private readonly Mock<IOrchestrationEngine> _mockBaseEngine;
    private readonly Mock<IStateProvider> _mockStateProvider;
    private readonly Mock<ILogger<TypeSafeStatefulOrchestrationEngine>> _mockLogger;
    private readonly TypeSafeStatefulOrchestrationEngine _engine;

    public TypeSafeStatefulOrchestrationEngineTests()
    {
        _mockBaseEngine = new Mock<IOrchestrationEngine>();
        _mockStateProvider = new Mock<IStateProvider>();
        _mockLogger = new Mock<ILogger<TypeSafeStatefulOrchestrationEngine>>();
        
        // TypeSafeOrchestrationEngine을 생성하기 위해 필요한 의존성들을 Mock으로 생성
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        var mockContextFactory = new Mock<IExecutionContextFactory>();
        var mockActionFactory = new Mock<IActionFactory>();
        var mockTypeSafeLogger = new Mock<ILogger<TypeSafeOrchestrationEngine>>();
        
        // Planner 함수 Mock 설정
        var mockPlannerFunction = new Mock<ILLMFunction>();
        var mockLLMResult = new Mock<ILLMResult>();
        mockLLMResult.Setup(x => x.Success).Returns(true);
        mockLLMResult.Setup(x => x.Content).Returns("Planning completed successfully");
        
        mockPlannerFunction.Setup(x => x.Role).Returns("planner");
        mockPlannerFunction.Setup(x => x.ExecuteAsync(It.IsAny<ILLMContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLLMResult.Object);
        
        mockLLMRegistry.Setup(x => x.IsRegistered("planner")).Returns(true);
        mockLLMRegistry.Setup(x => x.Resolve("planner")).Returns(mockPlannerFunction.Object);
        
        // Registry Mock 생성
        var mockRegistry = new Mock<IRegistry>();
        
        // ExecutionContext Mock 설정 - Registry를 포함하도록 수정
        var mockExecutionContext = new Mock<IExecutionContext>();
        mockExecutionContext.Setup(x => x.SessionId).Returns(Guid.NewGuid().ToString());
        mockExecutionContext.Setup(x => x.UserRequest).Returns("Test request");
        mockExecutionContext.Setup(x => x.Registry).Returns(mockRegistry.Object);
        mockExecutionContext.Setup(x => x.ExecutionHistory).Returns(new List<IExecutionStep>());
        mockExecutionContext.Setup(x => x.SharedData).Returns(new Dictionary<string, object>());
        
        mockContextFactory.Setup(x => x.CreateContext(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockExecutionContext.Object);
        
        var typeSafeEngine = new TypeSafeOrchestrationEngine(
            mockLLMRegistry.Object,
            mockToolRegistry.Object,
            mockContextFactory.Object,
            mockActionFactory.Object,
            mockTypeSafeLogger.Object
        );
        
        _engine = new TypeSafeStatefulOrchestrationEngine(
            typeSafeEngine,
            _mockStateProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ShouldNotThrow()
    {
        // Arrange
        var request = new Mock<IUserRequest>();
        request.Setup(x => x.Content).Returns("Test request");

        _mockStateProvider
            .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var act = () => _engine.ExecuteAsync(request.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_NullBaseEngine_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new TypeSafeStatefulOrchestrationEngine(
            null!, 
            _mockStateProvider.Object, 
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("baseEngine");
    }

    [Fact]
    public void Constructor_NullStateProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        var mockContextFactory = new Mock<IExecutionContextFactory>();
        var mockActionFactory = new Mock<IActionFactory>();
        var mockTypeSafeLogger = new Mock<ILogger<TypeSafeOrchestrationEngine>>();
        
        var typeSafeEngine = new TypeSafeOrchestrationEngine(
            mockLLMRegistry.Object,
            mockToolRegistry.Object,
            mockContextFactory.Object,
            mockActionFactory.Object,
            mockTypeSafeLogger.Object
        );

        // Act & Assert
        var act = () => new TypeSafeStatefulOrchestrationEngine(
            typeSafeEngine, 
            null!, 
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("stateProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        var mockContextFactory = new Mock<IExecutionContextFactory>();
        var mockActionFactory = new Mock<IActionFactory>();
        var mockTypeSafeLogger = new Mock<ILogger<TypeSafeOrchestrationEngine>>();
        
        var typeSafeEngine = new TypeSafeOrchestrationEngine(
            mockLLMRegistry.Object,
            mockToolRegistry.Object,
            mockContextFactory.Object,
            mockActionFactory.Object,
            mockTypeSafeLogger.Object
        );

        // Act & Assert
        var act = () => new TypeSafeStatefulOrchestrationEngine(
            typeSafeEngine, 
            _mockStateProvider.Object, 
            null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }
}