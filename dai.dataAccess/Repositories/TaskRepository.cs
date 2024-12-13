using dai.core.Models;
using dai.dataAccess.DbContext;
using dai.dataAccess.IRepositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.Repositories;  

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskModel>> GetAllTasksAsync()
    {
        return await _context.Tasks
                     .Include(t => t.taskInList)
                     //.Include(t => t.User) 
                     .ToListAsync();
    }

    public async Task<TaskModel> GetTaskByIdAsync(Guid id)
    {
        return await _context.Tasks
                     .Include(t => t.taskInList)
                     //.Include(t => t.User) 
                     .FirstOrDefaultAsync(t => t.Id == id);

        //var task = await _context.Tasks
        //                        .Include(t => t.taskInList)
        //                        .FirstOrDefaultAsync(t => t.Id == id);

        //if (task == null)
        //{
        //    return null;
        //}

        //var assignedEmails = new Dictionary<Guid, string>();

        //if (!string.IsNullOrEmpty(task.AssignedTo))
        //{
        //    var assignedUserIds = task.AssignedToList;
        //    var assignedUsers = await _context.Users
        //        .Where(u => assignedUserIds.Contains(u.Id))
        //        .Select(u => new { u.Id, u.Email })
        //        .ToListAsync();

        //    // Store the emails in a temporary property for use in the DTO
        //    assignedEmails = assignedUsers.ToDictionary(u => u.Id, u => u.Email);
        //}

        //return task;
    }

    public async Task<TaskModel> AddTaskAsync(TaskModel task, Guid listId)
    {
        var list = await _context.lists
                         .Include(l => l.taskInList)
                         .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null)
        {
            throw new NullReferenceException("List not found");
        }

        //task.Create_At = DateTime.Now;
        task.Update_At = DateTime.Now;
        //task.Position = list.taskInList.Count() + 1;
        task.Position = list.taskInList.Count(til => til.Task_Id.HasValue) + 1;
        task.AssignedTo = task.AssignedToList != null
                ? string.Join(",", task.AssignedToList)
                : string.Empty;

        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();

        var existingTaskInList = await _context.TaskInList
            .FirstOrDefaultAsync(til => til.List_Id == listId && til.Task_Id == null);

        if (existingTaskInList != null)
        {
            existingTaskInList.Task_Id = task.Id;
            existingTaskInList.Update_At = DateTime.Now;
            _context.TaskInList.Update(existingTaskInList);
        }
        else
        {
            var existingListInTaskInList = await _context.TaskInList
                .FirstOrDefaultAsync(til => til.List_Id == listId);

            Guid boardId = Guid.Empty;
            if (existingListInTaskInList != null)
            {
                boardId = existingListInTaskInList.Board_Id;
            }

            var newTaskInList = new TaskInListModel
            {
                Board_Id = boardId,
                Task_Id = task.Id,
                List_Id = listId,
                Create_At = DateTime.Now,
                Update_At = DateTime.Now,
                Permission = "default" 
            };

            _context.TaskInList.Add(newTaskInList);
        }

        await _context.SaveChangesAsync();

        list.NumberOfTaskInside++;
        _context.lists.Update(list);
        await _context.SaveChangesAsync();

        return task;
    }


    public async Task<TaskModel> UpdateTaskAsync(TaskModel task)
    {
        var existingTask = await _context.Tasks
                                         .Include(t => t.taskInList)
                                         .FirstOrDefaultAsync(t => t.Id == task.Id);
        if (existingTask == null)
        {
            throw new NullReferenceException("Task not found");
        }

        existingTask.Title = task.Title;
        existingTask.Description = task.Description;
        existingTask.Status = task.Status;
        existingTask.Create_At = task.Create_At;
        existingTask.Update_At = DateTime.Now;
        existingTask.Finish_At = task.Finish_At;
        //existingTask.AssignTo = task.AssignTo;
        existingTask.FileName = task.FileName; 
        existingTask.AvailableCheck = task.AvailableCheck;
        existingTask.AssignedTo = string.Join(",", task.AssignedToList);

        _context.Tasks.Update(existingTask);
        await _context.SaveChangesAsync();

        var taskInList = await _context.TaskInList
                                       .FirstOrDefaultAsync(t => t.Task_Id == task.Id);
        if (taskInList != null)
        {
            taskInList.Update_At = DateTime.Now;
            _context.TaskInList.Update(taskInList);
        }

        await _context.SaveChangesAsync();

        return existingTask;
    }

    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = await _context.Tasks.Include(t => t.taskInList).FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
        {
            throw new Exception("Task not found.");
        }

        var taskInListEntry = await _context.TaskInList.FirstOrDefaultAsync(til => til.Task_Id == taskId);
        if (taskInListEntry == null)
        {
            throw new Exception("TaskInList entry not found for the task.");
        }

        var listId = taskInListEntry.List_Id;

        _context.Tasks.Remove(task);
        _context.TaskInList.Remove(taskInListEntry);

        await _context.SaveChangesAsync(); 

        if (listId.HasValue)
        {
            var list = await _context.lists.FirstOrDefaultAsync(l => l.Id == listId.Value);
            if (list != null)
            {
                list.NumberOfTaskInside = Math.Max(0, list.NumberOfTaskInside - 1);
                _context.lists.Update(list);
                await _context.SaveChangesAsync();
            }
            var remainingTasksInList = await _context.TaskInList
                .Where(til => til.List_Id == listId.Value && til.Task_Id != null)
                .ToListAsync();

            if (!remainingTasksInList.Any())
            {
                var placeholderTaskInList = new TaskInListModel
                {
                    Id = Guid.NewGuid(),
                    List_Id = listId.Value,
                    Task_Id = null, 
                    Board_Id = taskInListEntry.Board_Id, 
                    Create_At = DateTime.UtcNow,
                    Update_At = DateTime.UtcNow,
                    Permission = "default" 
                };

                await _context.TaskInList.AddAsync(placeholderTaskInList);
                await _context.SaveChangesAsync();
            }
        }
    }



    public async Task<IEnumerable<TaskModel>> GetTasksByListIdAsync(Guid listId)
    {
        return await _context.Tasks
            .Where(task => task.taskInList.Any(l => l.List_Id == listId))
            .ToListAsync();
    }

    public async Task<int> CountTasksInListAsync(Guid listId)
    {
        return await _context.Tasks
            .Where(task => task.taskInList.Any(l => l.List_Id == listId))
            .CountAsync();
    }

    public async Task<IEnumerable<object>> GetUserTasksAsync(Guid userId)
    {
        // Step 1: Fetch all tasks with detailed information
        var userTasks = await _context.TaskInList
            .Where(t => t.Task != null) // Ensure task is not null
            .Include(t => t.Board)
                .ThenInclude(b => b.Collaborators)
            .Include(t => t.Board.Workspace) // Include workspace for ownership checks
            .Include(t => t.Task)
            .Select(t => new
            {
                TaskId = t.Task.Id,
                BoardId = t.Board.Id,
                Title = t.Task.Title,
                Description = t.Task.Description,
                Status = t.Task.Status,
                Create_At = t.Task.Create_At,
                Finish_At = t.Task.Finish_At,
                AvailableCheck = t.Task.AvailableCheck,
                Position = t.Task.Position,
                AssignedUsers = t.Task.AssignedToList,
                BoardName = t.Board.Name,
                HasCollaborators = t.Board.Collaborators.Any(),
                Collaborators = t.Board.Collaborators.Select(c => c.User_Id).ToList(),
                IsCollaborator = t.Board.Collaborators.Any(c => c.User_Id == userId),
                IsBoardOwner = t.Board.Workspace.UserId == userId 
            })
            .ToListAsync();

        // Step 2: Apply filtering logic
        var filteredTasks = userTasks
            .Where(t =>
                t.IsBoardOwner &&
                (!t.HasCollaborators) ||   (t.HasCollaborators && t.AssignedUsers.Contains(userId))
            )
            .ToList();

        // Step 3: Return sorted tasks
        return filteredTasks.OrderBy(t => t.Finish_At);
    }

    public async Task<IEnumerable<TaskModel>> GetTasksCreatedInMonthByBoardAsync(Guid boardId)
    {
        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;

        var tasks = await _context.Tasks
            .Where(t => 
                t.taskInList.Any(til => til.Board_Id == boardId) && t.Create_At.Month == currentMonth && t.Create_At.Year == currentYear)
            .ToListAsync();

        return tasks;
    }

    public async Task<IEnumerable<TaskModel>> GetTasksByStatusInMonthByBoardAsync(string status, Guid boardId)
    {
        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;

        var tasks = await _context.Tasks
            .Where(t => t.Status == status && 
                t.taskInList.Any(til => til.Board_Id == boardId) && t.Update_At.Month == currentMonth && t.Update_At.Year == currentYear)
            .ToListAsync();

        return tasks;
    }


    public async Task<(string WorkspaceName, string BoardName, string ListName, Guid BoardId)> GetTaskDetailsAsync(Guid taskId)
    {
        var taskInList = await _context.TaskInList
            .Include(t => t.List)
            .Include(t => t.Board)
            .ThenInclude(b => b.Workspace)
            .FirstOrDefaultAsync(t => t.Task_Id == taskId);

        if (taskInList == null || taskInList.Board == null || taskInList.List == null || taskInList.Board.Workspace == null)
        {
            return (null, null, null, Guid.Empty);
        }

        return (
            WorkspaceName: taskInList.Board.Workspace.Name,
            BoardName: taskInList.Board.Name,
            ListName: taskInList.List.Title,
            BoardId : taskInList.Board.Id
        );
    }

}
