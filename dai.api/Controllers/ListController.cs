using dai.core.DTO.List;
using dai.core.DTO.Task;
using dai.core.DTO.User;
using dai.dataAccess.IRepositories;
using dai.core.Models;
using dai.dataAccess.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dai.core.DTO.DragAndDrop;
using dai.dataAccess.DbContext;

namespace dai.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ListController : ControllerBase
{
    private readonly ICollaboratorRepository _collaboratorRepository;
    private readonly IListRepository _listRepository;
    private readonly IDragAndDropRepository _dragAndDropRepository;
    private readonly AppDbContext _context;

    public ListController(IListRepository listRepo, IDragAndDropRepository dragAndDropRepository, AppDbContext context, ICollaboratorRepository collaboratorRepository)
    {
        this._listRepository = listRepo;
        _dragAndDropRepository = dragAndDropRepository;
        _context = context;
        _collaboratorRepository = collaboratorRepository;
    }

    private Guid? GetUserIdFromHeader()
    {
        if (Request.Headers.TryGetValue("UserId", out var userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }

    // GET: api/List
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GET_List>>> GetAllLists()
    {
        var lists = await _listRepository.GetAllListsAsync();

        var listDtos = lists.Select(l => new GET_List
        {
            Id = l.Id,
            Title = l.Title,
            Create_At = l.Create_At,
            Update_At = l.Update_At,
            Status = l.Status,
            Position = l.Position,
            NumberOfTaskInside = l.NumberOfTaskInside,
        }).ToList();

        return Ok(listDtos);
    }


    // GET: api/List/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<GET_List>> GetListById(Guid id)
    {
        var list = await _listRepository.GetListByIdAsync(id);
        if (list == null)
        {
            return NotFound();
        }
        var listDto = new GET_List
        {
            Id = list.Id,
            Title = list.Title,
            Create_At = list.Create_At,
            Update_At = list.Update_At,
            Status = list.Status,
            Position = list.Position,
            NumberOfTaskInside = list.NumberOfTaskInside,
        };

        return Ok(listDto);
    }

    [HttpGet("board/{boardId:guid}")]
    public async Task<ActionResult<IEnumerable<GET_ListAndTask>>> GetListAndTaskByBoardId(Guid boardId)
    {
        var userId = GetUserIdFromHeader();
        if (userId == null)
            return Unauthorized(new { message = "User not logged in." });

        try
        {
            // Fetch the board and determine if the user is the owner
            var board = await _context.Boards
                                      .Include(b => b.Workspace)
                                      .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return NotFound(new { message = "Board not found." });

            bool isOwner = board.Workspace.UserId == userId;

            // Retrieve lists and tasks using repository
            var listsAndTasks = await _listRepository.GetListsAndTasksByBoardIdAsync(boardId, userId.Value, isOwner);

            // Map to DTO
            var listAndTaskDtos = listsAndTasks.Select(l => new GET_ListAndTask
            {
                Id = l.Id,
                Title = l.Title,
                Create_At = l.Create_At,
                Update_At = l.Update_At,
                Status = l.Status,
                Position = l.Position,
                NumberOfTaskInside = l.NumberOfTaskInside,
                TaskInside = l.taskInList.Select(t => new GET_Task
                {
                    Id = t.Task.Id,
                    Title = t.Task.Title,
                    Create_At = t.Task.Create_At,
                    Update_At = t.Task.Update_At,
                    Finish_At = t.Task.Finish_At,
                    Description = t.Task.Description,
                    Status = t.Task.Status,
                    Position = t.Task.Position,
                    AvailableCheck = t.Task.AvailableCheck,
                    AssignedUsers = t.Task.AssignedToList,
                    FileLink = t.Task.FileName
                }).ToList()
            }).ToList();

            return Ok(listAndTaskDtos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "An error occurred while retrieving tasks.");
        }
    }




    //[HttpGet("board/{boardId:guid}")]
    //public async Task<ActionResult<IEnumerable<GET_ListAndTask>>> GetListAndTaskByBoardId(Guid boardId)
    //{
    //    var userId = GetUserIdFromHeader();
    //    if (userId == null)
    //        return Unauthorized(new { message = "User not logged in." });

    //    try
    //    {
    //        var board = await _context.Boards.Include(b => b.Workspace).FirstOrDefaultAsync(b => b.Id == boardId);
    //        if (board == null)
    //            return NotFound(new { message = "Board not found." });

    //        var isOwner = board.Workspace.UserId == userId;
    //        var isEditor = await _context.Collaborators
    //            .AnyAsync(c => c.Board_Id == boardId && c.User_Id == userId && c.Permission == "Editor");

    //        if (!isOwner && !isEditor)
    //            return StatusCode(403, new { message = "You do not have permission to access this board." });

    //        var listAndTask = await _listRepository.GetListsAndTasksByBoardIdAsync(boardId);
    //        var listAndTaskDtos = listAndTask?.Select(l => new GET_ListAndTask
    //        {
    //            Id = l.Id,
    //            Title = l.Title,
    //            Create_At = l.Create_At,
    //            Update_At = l.Update_At,
    //            Status = l.Status,
    //            Position = l.Position,
    //            NumberOfTaskInside = l.NumberOfTaskInside,
    //            TaskInside = l.taskInList
    //                .Where(t => t.Task != null)
    //                .Select(t => new GET_Task
    //                {
    //                    Id = t.Task.Id,
    //                    Title = t.Task.Title,
    //                    Create_At = t.Task.Create_At,
    //                    Update_At = t.Task.Update_At,
    //                    Finish_At = t.Task.Finish_At,
    //                    Description = t.Task.Description,
    //                    Status = t.Task.Status,
    //                    Position = t.Task.Position,
    //                    AvailableCheck = t.Task.AvailableCheck,
    //                    AssignedUsers = t.Task.AssignedToList,
    //                    AssignedUsersEmails = t.Task.AssignedToList.ToDictionary(
    //                        userId => userId,
    //                        userId => _context.Users.FirstOrDefault(u => u.Id == userId) ? .Email),
    //                    //UserEmail = t.Task.User?.Email,
    //                    //UserEmailId = t.Task.User?.Id ?? Guid.Empty,
    //                    FileLink = t.Task.FileName
    //                }).ToList()
    //        }).ToList() ?? new List<GET_ListAndTask>(); // Trả về danh sách rỗng nếu không có dữ liệu

    //        return Ok(listAndTaskDtos);
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.Error.WriteLine($"Error: {ex.Message}");
    //        return StatusCode(500, "An error occurred while processing your request.");
    //    }
    //}

    // POST: api/List
    [HttpPost]
    public async Task<ActionResult<POST_List>> PostList(POST_List postList, Guid boardId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var list = new ListModel
        {
            Id = Guid.NewGuid(),
            Title = postList.Title,
            Create_At = DateTime.Now,
            Update_At = DateTime.Now,
            Status = postList.Status,
            taskInList = new List<TaskInListModel>()
        };

        try
        {
            var createdList = await _listRepository.AddListAsync(list, boardId);

            var listDTO = new ListModel
            {
                Id = createdList.Id,
                Title = createdList.Title,
                Create_At = createdList.Create_At,
                Update_At = createdList.Update_At,
                Status = createdList.Status,
                Position = createdList.Position,

            };
            await Console.Out.WriteLineAsync("you just create a list");
            return CreatedAtAction(nameof(GetListById), new { id = listDTO.Id }, listDTO);
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, "An error occurred while saving the list. Please try again.");
        }
    }

    // PUT: api/List/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateList(Guid id, PUT_List postList)
    {
        var existingList = await _listRepository.GetListByIdAsync(id);

        if (existingList == null)
        {
            return NotFound();
        }

        existingList.Title = postList.Title;
        existingList.Update_At = DateTime.Now;
        existingList.Status = postList.Status;

        try
        {
            await _listRepository.UpdateListAsync(existingList);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (await _listRepository.GetListByIdAsync(id) == null)
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    //Delete API
    [HttpDelete("{listId}")]
    public async Task<IActionResult> DeleteList(Guid listId)
    {
        try
        {
            await _listRepository.DeleteListAsync(listId);
            Console.WriteLine("mother fucker xoa duoc roi");
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log the exception (not shown here for brevity)
            Console.WriteLine("mother fucker khong xoa duoc!!! FUCK");
            return StatusCode(500, "Internal server error");
        }
    }



    [HttpPut("move")]
    public async Task<IActionResult> MoveList([FromBody] MoveListRequest request)
    {
        if (request == null || request.DraggedListId == Guid.Empty || request.TargetListId == Guid.Empty)
        {
            return BadRequest("Invalid request");
        }

        var listToMove = await _dragAndDropRepository.GetListByIdAsync(request.DraggedListId);
        var targetList = await _dragAndDropRepository.GetListByIdAsync(request.TargetListId);

        if (listToMove == null || targetList == null)
        {
            return NotFound("List not found");
        }

        // Update the order of the lists
        await _dragAndDropRepository.UpdateListOrder(listToMove, targetList);
        Console.WriteLine("change list order");

        return Ok();
    }

}