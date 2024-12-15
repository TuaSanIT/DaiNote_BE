using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly ITaskRepository _taskRepository;

    public PdfController(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    [HttpGet("generate-report")]
    public async Task<IActionResult> GenerateReport(Guid boardId)
    {
        var tasksCreatedThisMonth = await _taskRepository.GetTasksCreatedInMonthByBoardAsync(boardId);
        var tasksDoneThisMonth = await _taskRepository.GetTasksByStatusInMonthByBoardAsync("done", boardId);
        var tasksOverThisMonth = await _taskRepository.GetTasksByStatusInMonthByBoardAsync("over", boardId);

        var reportData = new
        {
            TasksCreatedThisMonth = tasksCreatedThisMonth,
            TasksDoneThisMonth = tasksDoneThisMonth,
            TasksOverThisMonth = tasksOverThisMonth
        };

        return Ok(reportData);
    }
}

