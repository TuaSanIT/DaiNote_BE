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

public class ListRepository : IListRepository
{
    private readonly AppDbContext _context;

    public ListRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ListModel>> GetAllListsAsync()
    {
        return await _context.lists.Include(l => l.taskInList).ToListAsync();
    }

    public async Task<ListModel> GetListByIdAsync(Guid id)
    {
        return await _context.lists.Include(l => l.taskInList)
                                   .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<IEnumerable<ListModel>> GetListsByBoardIdAsync(Guid boardId)
    {
        return await _context.TaskInList
                             .Where(til => til.Board_Id == boardId && til.List_Id.HasValue)
                             .Select(til => til.List)
                             .Distinct() // To avoid duplicate lists if they have multiple tasks
                             .ToListAsync();
    }

    public async Task<IEnumerable<ListModel>> GetListsAndTasksByBoardIdAsync(Guid boardId)
    {
        return await _context.lists
                                    .Where(l => l.taskInList.Any(t => t.Board_Id == boardId))
                                    .Include(l => l.taskInList)
                                        .ThenInclude(til => til.Task)
                                            //.ThenInclude(t => t.User)
                                    .OrderBy(l => l.Position) // Order lists by position
                                    .Select(l => new ListModel
                                    {
                                        Id = l.Id,
                                        Title = l.Title,
                                        Status = l.Status,
                                        Position = l.Position,
                                        NumberOfTaskInside = l.NumberOfTaskInside,
                                        Create_At = l.Create_At,
                                        Update_At = l.Update_At,
                                        taskInList = l.taskInList
                                            .OrderBy(til => til.Task.Position) // Order tasks by position
                                            .Select(til => new TaskInListModel
                                            {
                                                Id = til.Id,
                                                Board_Id = til.Board_Id,
                                                Task_Id = til.Task_Id,
                                                List_Id = til.List_Id,
                                                Create_At = til.Create_At,
                                                Update_At = til.Update_At,
                                                Permission = til.Permission,
                                                Task = til.Task == null ? null : new TaskModel
                                                {
                                                    Id = til.Task.Id,
                                                    Title = til.Task.Title,
                                                    Create_At = til.Task.Create_At,
                                                    Update_At = til.Task.Update_At,
                                                    Finish_At = til.Task.Finish_At,
                                                    Description = til.Task.Description,
                                                    Status = til.Task.Status,
                                                    Position = til.Task.Position,
                                                    AvailableCheck = til.Task.AvailableCheck,
                                                    AssignedTo = til.Task.AssignedTo,
                                                    AssignedToList = til.Task.AssignedToList,
                                                    //User = til.Task.User == null ? null : new UserModel
                                                    //{
                                                    //    Email = til.Task.User.Email,
                                                    //    Id = til.Task.User.Id,
                                                    //},
                                                    
                                                    FileName = til.Task.FileName
                                                }
                                            })
                                            .ToList()
                                    })
                                    .ToListAsync();
    }


    public async Task<ListModel> AddListAsync(ListModel list, Guid boardId)
    {
        var board = await _context.Boards
                          .Include(b => b.taskInList)
                          .FirstOrDefaultAsync(b => b.Id == boardId);

        if (board == null)
        {
            throw new NullReferenceException("Board not found");
        }

        list.Create_At = DateTime.Now;
        list.Update_At = DateTime.Now;
        list.Position = board.NumberOfListInside + 1;
        list.NumberOfTaskInside = 0;

        await _context.lists.AddAsync(list);
        await _context.SaveChangesAsync();

        // Add the corresponding TaskInList entry
        var taskInList = new TaskInListModel
        {
            List_Id = list.Id,
            Board_Id = boardId,
            Task_Id = null, // Task_Id is nullable
            Create_At = DateTime.Now,
            Update_At = DateTime.Now,
            Permission = "default" // or any default value
        };

        await _context.TaskInList.AddAsync(taskInList);
        await _context.SaveChangesAsync();

        // Update NumberOfListInside in the board
        board.NumberOfListInside++;
        _context.Boards.Update(board);
        await _context.SaveChangesAsync();

        return list;
    }

    public async Task<ListModel> UpdateListAsync(ListModel list)
    {
        var existingList = await _context.lists
                                         .FirstOrDefaultAsync(l => l.Id == list.Id);

        if (existingList == null)
        {
            throw new NullReferenceException("List not found");
        }

        existingList.Title = list.Title;
        existingList.Update_At = DateTime.Now;
        existingList.Status = list.Status;

        _context.lists.Update(existingList);
        await _context.SaveChangesAsync();

        // Update TaskInList
        if (existingList.taskInList != null)
        {
            foreach (var taskInList in existingList.taskInList)
            {
                taskInList.Update_At = DateTime.Now;
                _context.TaskInList.Update(taskInList);
                await _context.SaveChangesAsync();
            }
        }

        return existingList;
    }


    public async Task DeleteListAsync(Guid listId)
    {
        var list = await GetListByIdAsync(listId);
        if (list != null)
        {
            var boardId = list.taskInList.FirstOrDefault()?.Board_Id;

            // Delete all task IDs in the list
            var taskIds = await _context.TaskInList
                                        .Where(t => t.List_Id == listId)
                                        .Select(t => t.Task_Id)
                                        .ToListAsync();

            // Delete all tasks in the list
            var tasks = await _context.Tasks
                                      .Where(t => taskIds.Contains(t.Id))
                                      .ToListAsync();
            _context.Tasks.RemoveRange(tasks);

            // Delete all TaskInList entries related to the list
            var taskInListEntries = await _context.TaskInList
                                                  .Where(t => t.List_Id == listId)
                                                  .ToListAsync();
            _context.TaskInList.RemoveRange(taskInListEntries);
            await _context.SaveChangesAsync();

            // Delete the list itself
            _context.lists.Remove(list);

            await _context.SaveChangesAsync();

            if (boardId.HasValue)
            {
                // Adjust positions of remaining lists
                var listsToUpdate = await _context.lists
                    .Where(l => l.taskInList.Any(til => til.Board_Id == boardId.Value && l.Position > list.Position))
                    .ToListAsync();

                foreach (var l in listsToUpdate)
                {
                    l.Position--;
                    _context.lists.Update(l);
                }

                // Update NumberOfListInside in the board
                var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId.Value);
                if (board != null)
                {
                    board.NumberOfListInside--;
                    _context.Boards.Update(board);
                }

                await _context.SaveChangesAsync();
            }
        }
    }



}
