using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Orchestration;
using Microsoft.AspNetCore.Mvc;

namespace AIAgentFramework.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrchestrationController : ControllerBase
{
    private readonly IOrchestrationEngine _orchestrationEngine;

    public OrchestrationController(IOrchestrationEngine orchestrationEngine)
    {
        _orchestrationEngine = orchestrationEngine ?? throw new ArgumentNullException(nameof(orchestrationEngine));
    }

    [HttpPost("execute")]
    public async Task<ActionResult<IOrchestrationResult>> Execute([FromBody] ExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserRequest))
        {
            return BadRequest("사용자 요청이 필요합니다.");
        }

        var userRequest = new UserRequest(request.UserRequest)
        {
            UserId = request.UserId ?? "anonymous"
        };

        var result = await _orchestrationEngine.ExecuteAsync(userRequest);
        return Ok(result);
    }

    [HttpPost("continue")]
    public async Task<ActionResult<IOrchestrationResult>> Continue([FromBody] ContinueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest("세션 ID가 필요합니다.");
        }

        var context = new OrchestrationContext(request.SessionId, request.UserRequest ?? "");
        var result = await _orchestrationEngine.ContinueAsync(context);
        return Ok(result);
    }

    [HttpGet("status/{sessionId}")]
    public ActionResult<SessionStatus> GetStatus(string sessionId)
    {
        return Ok(new SessionStatus
        {
            SessionId = sessionId,
            IsCompleted = false,
            CurrentStep = "진행 중"
        });
    }
}

public class ExecuteRequest
{
    public string UserRequest { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

public class ContinueRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserRequest { get; set; }
}

public class SessionStatus
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
}