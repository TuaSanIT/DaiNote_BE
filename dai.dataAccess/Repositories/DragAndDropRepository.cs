using dai.core.Models;
using dai.dataAccess.DbContext;
using dai.dataAccess.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.Repositories;

public class DragAndDropRepository : IDragAndDropRepository
{
    private readonly AppDbContext _context;

    public DragAndDropRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ListModel> GetListByIdAsync(Guid listId)
    {
        return await _context.lists.FindAsync(listId);
    }

    public async Task<TaskModel> GetTaskByIdAsync(Guid taskId)
    {
        return await _context.Tasks.FindAsync(taskId);
    }

    public async Task UpdateTaskOrder(TaskModel taskToMove, TaskModel targetTask)
    {
        // Get the list ID from the TaskInList table
        var listId = (await _context.TaskInList.FirstOrDefaultAsync(t => t.Task_Id == taskToMove.Id))?.List_Id;

        if (listId == null)
        {
            throw new Exception("Could not find List entry for the provided Task.");
        }

        // Fetch all tasks for the same list, ordered by their current positions
        var tasks = await _context.TaskInList
            .Where(t => t.List_Id == listId)
            .Select(t => t.Task)
            .OrderBy(t => t.Position)
            .ToListAsync();

        int newPosition = targetTask.Position;
        int originalPosition = taskToMove.Position;

        if (originalPosition < newPosition)
        {
            // Shift tasks between originalPosition and newPosition to the left
            foreach (var task in tasks.Where(t => t.Position > originalPosition && t.Position <= newPosition))
            {
                task.Position--; // Shift left
            }
        }
        else if (originalPosition > newPosition)
        {
            // Shift tasks between newPosition and originalPosition to the right
            foreach (var task in tasks.Where(t => t.Position >= newPosition && t.Position < originalPosition))
            {
                task.Position++; // Shift right
            }
        }

        // Update the position of the task being moved
        taskToMove.Position = newPosition;

        // Save all changes
        _context.Tasks.UpdateRange(tasks); // Update all tasks
        _context.Tasks.Update(taskToMove); // Also update the moved task
        await _context.SaveChangesAsync();
    }


    public async Task UpdateListOrder(ListModel listToMove, ListModel targetList)
    {
        //// Get the board ID from the TaskInList table
        var boardId = (await _context.TaskInList.FirstOrDefaultAsync(t => t.List_Id == listToMove.Id))?.Board_Id;

        if (boardId == null)
        {
            throw new Exception("Could not find Board entry for the provided List.");
        }

        // Fetch all lists for the same board, ordered by their current positions
        var lists = await _context.lists
                                        .Where(l => l.taskInList.Any(t => t.Board_Id == boardId))
                                        .OrderBy(l => l.Position)
                                        .ToListAsync();

        int newPosition = targetList.Position;
        int originalPosition = listToMove.Position;

        if (originalPosition < newPosition)
        {
            // Shift lists between originalPosition and newPosition to the left
            foreach (var list in lists.Where(l => l.Position > originalPosition && l.Position <= newPosition))
            {
                list.Position--; // Shift left
            }
        }
        else if (originalPosition > newPosition)
        {
            // Shift lists between newPosition and originalPosition to the right
            foreach (var list in lists.Where(l => l.Position >= newPosition && l.Position < originalPosition))
            {
                list.Position++; // Shift right
            }
        }

        // Update the position of the list being moved
        listToMove.Position = newPosition;

        _context.lists.UpdateRange(lists); // Update all lists
        _context.lists.Update(listToMove); // Also update the moved list
        await _context.SaveChangesAsync();
    }

    public async Task MoveTaskToList(TaskModel taskToMove, ListModel targetList)
    {
        var findList = await _context.lists.FirstOrDefaultAsync(t => t.Id == targetList.Id);
        if (findList == null)
        {
            throw new Exception("Target list not found.");
        }

        // Check if the task exists in any list
        var taskInList = await _context.TaskInList.FirstOrDefaultAsync(t => t.Task_Id == taskToMove.Id);
        if (taskInList == null)
        {
            throw new Exception("Task not found in any list.");
        }

        var oldListId = taskInList.List_Id;
        if (oldListId == targetList.Id)
        {
            throw new Exception("Task is already in the target list.");
        }

        // Check the count of tasks in the old list
        var oldListTasksCount = await _context.TaskInList.CountAsync(t => t.List_Id == oldListId);

        // Reorder the tasks in the old list
        if (oldListTasksCount > 1)
        {
            var tasksInOldList = await _context.Tasks
                .Where(t => t.taskInList.Any(tl => tl.List_Id == oldListId) && t.Position > taskToMove.Position)
                .ToListAsync();

            foreach (var task in tasksInOldList)
            {
                task.Position--;
                _context.Tasks.Update(task);
            }
        }

        // Update the TaskInList entry to move the task to the new list
        taskInList.List_Id = targetList.Id;

        // Handle the case where the target list has no tasks
        var targetListTasksCount = await _context.TaskInList.CountAsync(t => t.List_Id == targetList.Id);
        if (targetListTasksCount == 1)
        {
            // Find the TaskInList entry with null Task_Id for the target list
            var taskInListWithNullTaskId = await _context.TaskInList.FirstOrDefaultAsync(t => t.List_Id == targetList.Id && t.Task_Id == null);
            if (taskInListWithNullTaskId != null)
            {
                // Delete the TaskInList entry with null Task_Id
                _context.TaskInList.Remove(taskInListWithNullTaskId);
            }

            // Set the position of the moved task to the first position in the target list
            //taskToMove.Position = 1;
            taskToMove.Position = targetList.NumberOfTaskInside + 1;
        }
        else
        {
            // Set the position of the moved task to the last position in the target list
            taskToMove.Position = targetList.NumberOfTaskInside + 1;
        }

        // Update new list's task count
        targetList.NumberOfTaskInside++;
        _context.lists.Update(targetList);

        // Update old list's task count
        if (oldListId != null)
        {
            var oldList = await _context.lists.FindAsync(oldListId);
            if (oldList != null)
            {
                oldList.NumberOfTaskInside--; // Decrement task count in the old list
                _context.lists.Update(oldList);

                // If the old list is now empty, add a new TaskInList entry with null Task_Id
                if (oldList.NumberOfTaskInside == 0)
                {
                    var newTaskInListWithNullTaskId = new TaskInListModel
                    {
                        Id = Guid.NewGuid(),
                        Board_Id = taskInList.Board_Id,
                        List_Id = oldListId,
                        Task_Id = null,
                        Create_At = DateTime.UtcNow,
                        Update_At = DateTime.UtcNow,
                        Permission = "default"
                    };
                    await _context.TaskInList.AddAsync(newTaskInListWithNullTaskId);
                }
            }
        }

        // Save changes
        _context.Tasks.Update(taskToMove);
        _context.TaskInList.Update(taskInList);
        await _context.SaveChangesAsync();
    }

    public async Task MoveTaskToAnotherListAsync(Guid draggedTaskId, Guid targetTaskId)
    {
        try
        {
            // Get the dragged task and target task
            var draggedTask = await _context.Tasks.FindAsync(draggedTaskId);
            var targetTask = await _context.Tasks.FindAsync(targetTaskId);

            if (draggedTask == null)
            {
                throw new Exception("Dragged task not found.");
            }

            if (targetTask == null)
            {
                throw new Exception("Target task not found.");
            }

            // Get the TaskInList entries
            var oldTaskInList = await _context.TaskInList.FirstOrDefaultAsync(t => t.Task_Id == draggedTaskId);
            var newTaskInList = await _context.TaskInList.FirstOrDefaultAsync(t => t.Task_Id == targetTaskId);

            if (oldTaskInList == null)
            {
                throw new Exception("Old task's list not found.");
            }

            if (newTaskInList == null)
            {
                throw new Exception("New task's list not found.");
            }

            var oldListId = oldTaskInList.List_Id;
            var newListId = newTaskInList.List_Id;

            if (oldListId == newListId)
            {
                throw new Exception("Dragged task is already in the target list.");
            }

            // Fetch the old and new lists
            var oldList = await _context.lists.FindAsync(oldListId);
            var newList = await _context.lists.FindAsync(newListId);

            if (oldList == null)
            {
                throw new Exception("Old list not found.");
            }

            if (newList == null)
            {
                throw new Exception("New list not found.");
            }

            // Update positions in the old list
            var oldListTasks = await _context.TaskInList
                .Where(t => t.List_Id == oldListId && t.Task_Id != draggedTaskId)
                .Select(t => t.Task)
                .OrderBy(t => t.Position)
                .ToListAsync();

            if (oldListTasks == null)
            {
                throw new Exception("Old list tasks not found.");
            }

            foreach (var task in oldListTasks.Where(t => t.Position > draggedTask.Position))
            {
                task.Position--; // Shift position left
            }

            // Update positions in the new list
            var newListTasks = await _context.TaskInList
                .Where(t => t.List_Id == newListId && t.Task != null)
                .Select(t => t.Task)
                .OrderBy(t => t.Position)
                .ToListAsync();

            if (newListTasks == null || !newListTasks.Any())
            {
                throw new Exception("New list tasks not found or empty.");
            }

            // Set the new position for the dragged task
            int targetPosition = targetTask.Position;
            draggedTask.Position = targetPosition; // Position is after the target task

            // Shift positions of the tasks in the new list to the right
            foreach (var task in newListTasks.Where(t => t.Position >= draggedTask.Position))
            {
                if (task.Position == null) // Guard against null positions
                {
                    throw new Exception($"Task with ID {task.Id} has a null position.");
                }
                task.Position++; 
            }

            // Update TaskInList entry
            oldTaskInList.List_Id = newListId; // Move task to the new list

            // Update the task counts
            oldList.NumberOfTaskInside--; // Decrement old list's task count
            newList.NumberOfTaskInside++; // Increment new list's task count

            // Save changes
            _context.Tasks.Update(draggedTask); // Update the dragged task
            _context.TaskInList.Update(oldTaskInList); // Update the TaskInList entry
            _context.lists.Update(oldList); // Update the old list
            _context.lists.Update(newList); // Update the new list

            await _context.SaveChangesAsync();

            // Check if the old list is now empty and insert a placeholder entry if needed
            if (oldList.NumberOfTaskInside == 0)
            {
                var newTaskInListWithNullTaskId = new TaskInListModel
                {
                    Id = Guid.NewGuid(),
                    List_Id = oldListId,
                    Task_Id = null,
                    Board_Id = oldTaskInList.Board_Id,
                    Create_At = DateTime.UtcNow,
                    Update_At = DateTime.UtcNow,
                    Permission = "default"
                };
                await _context.TaskInList.AddAsync(newTaskInListWithNullTaskId);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Log detailed error message
            Console.WriteLine($"Error in MoveTaskToAnotherListAsync: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            // Rethrow exception to be caught by the calling method
            throw;
        }
    }






}