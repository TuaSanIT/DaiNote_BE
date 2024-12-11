using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories;

public interface IDragAndDropRepository
{
    Task<ListModel> GetListByIdAsync(Guid listId);
    Task<TaskModel> GetTaskByIdAsync(Guid taskId);
    Task UpdateTaskOrder(TaskModel taskToMove, TaskModel targetTask);
    Task UpdateListOrder(ListModel listToMove, ListModel targetList);
    Task MoveTaskToList(TaskModel taskToMove, ListModel targetList);
    Task MoveTaskToAnotherListAsync(Guid draggedTaskId, Guid targetTaskId);
}
