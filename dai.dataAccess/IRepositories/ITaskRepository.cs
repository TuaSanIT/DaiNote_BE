using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories;

public interface ITaskRepository
{
    Task<IEnumerable<TaskModel>> GetAllTasksAsync();
    Task<TaskModel> GetTaskByIdAsync(Guid id);
    Task<TaskModel> AddTaskAsync(TaskModel task, Guid listId);
    Task<TaskModel> UpdateTaskAsync(TaskModel task);
    Task DeleteTaskAsync(Guid id);

    Task<IEnumerable<TaskModel>> GetTasksByListIdAsync(Guid listId);
    Task<int> CountTasksInListAsync(Guid listId);

    Task<IEnumerable<object>> GetUserTasksAsync(Guid userId);

    Task<IEnumerable<TaskModel>> GetTasksCreatedInMonthByBoardAsync(Guid boardId);
    Task<IEnumerable<TaskModel>> GetTasksByStatusInMonthByBoardAsync(string status, Guid boardId);
    Task<(string WorkspaceName, string BoardName, string ListName, Guid BoardId)> GetTaskDetailsAsync(Guid taskId);
}
