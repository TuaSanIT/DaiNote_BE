using AutoMapper;
using dai.core.DTO.Board;
using dai.core.Models;
using dai.dataAccess.DbContext;
using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BoardController : ControllerBase
    {
        private readonly IBoardRepository _boardRepository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public BoardController(IBoardRepository boardRepository, IMapper mapper, AppDbContext context)
        {
            _boardRepository = boardRepository;
            _mapper = mapper;
            _context = context;
        }

        private Guid? GetUserIdFromHeader()
        {
            if (Request.Headers.TryGetValue("UserId", out var userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return null;
        }

        private async Task<(bool IsAuthorized, bool IsOwner, bool IsEditor)> CheckAuthorization(Guid boardId, Guid userId)
        {
            var board = await _boardRepository.GetBoardByIdAsync(boardId);
            if (board == null) return (false, false, false);


            var workspaceOwner = await _context.Workspaces
                .Where(w => w.Id == board.WorkspaceId)
                .Select(w => w.UserId)
                .FirstOrDefaultAsync();

            bool isOwner = workspaceOwner == userId;


            bool isEditor = await _context.Collaborators
                .AnyAsync(c => c.Board_Id == boardId && c.User_Id == userId && c.Permission == "Editor");

            return (isOwner || isEditor, isOwner, isEditor);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BoardDto>>> GetAllBoards()
        {
            try
            {
                var boards = await _boardRepository.GetAllBoardsAsync();


                var boardDtos = _mapper.Map<IEnumerable<BoardDto>>(boards);

                return Ok(boardDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("{boardId}")]
        public async Task<ActionResult<BoardDto>> GetBoardById(Guid boardId)
        {
            var userId = GetUserIdFromHeader();
            if (userId == null) return Unauthorized(new { message = "User not logged in." });

            var (isAuthorized, isOwner, isEditor) = await CheckAuthorization(boardId, userId.Value);

            if (!isAuthorized)
                return StatusCode(403, new { message = "You do not have permission to access this board." });

            var board = await _boardRepository.GetBoardByIdAsync(boardId);
            if (board == null) return NotFound(new { message = "Board not found." });

            var boardDto = _mapper.Map<BoardDto>(board);
            return Ok(new
            {
                board = boardDto,
                isOwner,
                isEditor
            });
        }


        [HttpPost("{workspaceId}")]
        public async Task<ActionResult<BoardDto>> PostBoard(Guid workspaceId, [FromBody] CreateBoardDto createBoardDto)
        {
            var userId = GetUserIdFromHeader();
            if (userId == null) return Unauthorized(new { message = "User not logged in." });

            var workspace = await _context.Workspaces.FindAsync(workspaceId);
            if (workspace == null || workspace.UserId != userId)
            {
                return StatusCode(403, new { message = "You do not have permission to create a board in this workspace." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            var userBoardCount = await _context.Boards
                .Where(b => b.Workspace.UserId == userId)
                .CountAsync();

            if (user.IsVipSupplier != true && userBoardCount >= 3)
            {
                return StatusCode(403, new { message = "You need to be a VIP to create more than 3 boards." });
            }

            var board = new BoardModel
            {
                Id = Guid.NewGuid(),
                Name = createBoardDto.Name,
                Status = createBoardDto.Status,
                WorkspaceId = workspaceId,
                Create_At = DateTime.UtcNow,
                Update_At = DateTime.UtcNow
            };

            try
            {
                var createdBoard = await _boardRepository.CreateBoardAsync(board);
                var boardDto = _mapper.Map<BoardDto>(createdBoard);
                return CreatedAtAction(nameof(GetBoardById), new { boardId = boardDto.Id }, boardDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPut("{boardId}")]
        public async Task<ActionResult> PutBoard(Guid boardId, [FromBody] UpdateBoardDto updateBoardDto)
        {
            var userId = GetUserIdFromHeader();
            if (userId == null) return Unauthorized(new { message = "User not logged in." });

            var (isAuthorized, isOwner, isEditor) = await CheckAuthorization(boardId, userId.Value);

            if (!isAuthorized)
                return StatusCode(403, new { message = "You do not have permission to access this board." });

            var board = await _boardRepository.GetBoardByIdAsync(boardId);
            if (board == null) return NotFound(new { message = "Board not found." });

            board.Name = updateBoardDto.Name ?? board.Name;
            board.Status = updateBoardDto.Status ?? board.Status;
            board.Update_At = DateTime.UtcNow;

            try
            {
                await _boardRepository.UpdateBoardAsync(board);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpDelete("{boardId}")]
        public async Task<IActionResult> DeleteBoard(Guid boardId)
        {
            var userId = GetUserIdFromHeader();
            if (userId == null) return Unauthorized(new { message = "User not logged in." });

            var (isAuthorized, isOwner, isEditor) = await CheckAuthorization(boardId, userId.Value);

            if (!isAuthorized)
                return StatusCode(403, new { message = "You do not have permission to access this board." });

            try
            {
                await _boardRepository.DeleteBoardAsync(boardId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound($"Board not found: {ex.Message}");
            }
        }


        [HttpGet("{boardId}/workspace")]
        public async Task<ActionResult<Guid>> GetWorkspaceIdByBoardId(Guid boardId)
        {
            var workspaceId = await _boardRepository.GetWorkspaceIdByBoardIdAsync(boardId);
            if (workspaceId == null)
            {
                return NotFound("Board not found");
            }

            return Ok(workspaceId);
        }


        [HttpGet("{boardId}/collaborators")]
        public async Task<IActionResult> GetCollaborators(Guid boardId)
        {
            var currentUserId = GetUserIdFromHeader();
            if (currentUserId == null)
                return Unauthorized(new { message = "User not logged in." });


            var board = await _context.Boards.Include(b => b.Workspace).FirstOrDefaultAsync(b => b.Id == boardId);
            if (board == null)
                return NotFound(new { message = "Board not found." });


            var isOwner = board.Workspace.UserId == currentUserId;


            var collaborators = await _context.Collaborators
                .Include(c => c.User)
                .Where(c => c.Board_Id == boardId)
                .Select(c => new
                {
                    c.User_Id,
                    c.User.FullName,
                    c.User.UserName,
                    c.User.AvatarImage,
                    Permission = "Editor" // Tất cả từ bảng Collab đều là Editor
                })
                .ToListAsync();


            if (isOwner)
            {
                collaborators.Add(new
                {
                    User_Id = board.Workspace.UserId,
                    FullName = "Owner",
                    UserName = "Owner",
                    AvatarImage = "/default-avatar.png",
                    Permission = "Owner"
                });
            }

            return Ok(new
            {
                isOwner,
                collaborators
            });
        }

        [HttpGet("joined")]
        public async Task<IActionResult> GetJoinedBoards(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid userId");
            }

            var boards = await _context.Collaborators
                .Where(c => c.User_Id == userId && c.Permission != "Pending")
                .Include(c => c.Board)
                .ThenInclude(b => b.Workspace)
                .Select(c => new
                {
                    Id = c.Board_Id,
                    Name = c.Board.Name,
                    Status = c.Board.Status,
                    WorkspaceName = c.Board.Workspace.Name
                })
                .ToListAsync();

            return Ok(boards);
        }

        [HttpDelete("{boardId}/collaborator/{userId}")]
        public async Task<IActionResult> RemoveCollaborator(Guid boardId, Guid userId)
        {
            var currentUserId = GetUserIdFromHeader();
            if (currentUserId == null)
                return Unauthorized(new { message = "User not logged in." });


            var board = await _context.Boards.Include(b => b.Workspace).FirstOrDefaultAsync(b => b.Id == boardId);
            if (board == null)
                return NotFound(new { message = "Board not found." });


            if (board.Workspace.UserId != currentUserId)
                return StatusCode(403, new { message = "Only the Owner can remove collaborators." });


            var collaborator = await _context.Collaborators
                .FirstOrDefaultAsync(c => c.Board_Id == boardId && c.User_Id == userId);

            if (collaborator == null)
                return NotFound(new { message = "Collaborator not found." });

            _context.Collaborators.Remove(collaborator);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Collaborator removed successfully." });
        }

        [HttpPost("{boardId}/import")]
        public async Task<IActionResult> ImportExcel(Guid boardId, IFormFile file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // giay phep

            if (file == null || file.Length == 0)
                return BadRequest("File is not provided or empty.");

            var board = await _context.Boards
                .Include(b => b.taskInList)
                    .ThenInclude(t => t.Task)
                .Include(b => b.taskInList)
                    .ThenInclude(t => t.List)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return NotFound("Board not found.");

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    return BadRequest("Invalid Excel file format.");

                var rows = worksheet.Dimension.Rows;
                var columns = worksheet.Dimension.Columns;

                // ko lay row thua
                int nonEmptyColumns = Enumerable.Range(1, columns)
                    .Count(col => !string.IsNullOrWhiteSpace(worksheet.Cells[1, col]?.Text));

                bool isDetailed = nonEmptyColumns == 7;

                if (!isDetailed)
                    return BadRequest("Unsupported Excel template. Please upload a valid file.");

                var existingLists = board.taskInList.Select(t => t.List).Distinct().ToList();
                var lists = new List<ListModel>();
                var tasks = new List<TaskInListModel>();
                ListModel currentList = null;

                for (int row = 2; row <= rows; row++)
                {
                    var listTitle = worksheet.Cells[row, 1].Text.Trim();
                    var listStatus = isDetailed ? worksheet.Cells[row, 2].Text.Trim() : "Active";

                    if (!string.IsNullOrWhiteSpace(listTitle))
                    {
                        currentList = existingLists.FirstOrDefault(l => l.Title == listTitle);

                        if (currentList == null)
                        {
                            currentList = new ListModel
                            {
                                Id = Guid.NewGuid(),
                                Title = listTitle,
                                Create_At = DateTime.UtcNow,
                                Update_At = DateTime.UtcNow,
                                Status = listStatus,
                                Position = board.NumberOfListInside + 1,
                                NumberOfTaskInside = 0
                            };
                            lists.Add(currentList);
                            board.NumberOfListInside++;
                        }
                        else
                        {
                            currentList.Update_At = DateTime.UtcNow;
                        }
                    }

                    if (currentList == null)
                        return BadRequest($"Task found before defining a list at row {row}.");

                    var taskTitle = isDetailed ? worksheet.Cells[row, 3].Text.Trim() : worksheet.Cells[row, 2].Text.Trim();
                    var taskDescription = isDetailed ? worksheet.Cells[row, 6].Text.Trim() : worksheet.Cells[row, 3].Text.Trim();
                    var taskCreateAt = isDetailed ? worksheet.Cells[row, 4].Text.Trim() : DateTime.UtcNow.ToString();
                    var taskFinishAt = isDetailed ? worksheet.Cells[row, 5].Text.Trim() : DateTime.UtcNow.AddDays(7).ToString();
                    var taskStatus = isDetailed ? worksheet.Cells[row, 7].Text.Trim() : "Pending";

                    var assignedTo = isDetailed ? worksheet.Cells[row, 8].Text.Trim() : string.Empty;

                    var assignedToList = !string.IsNullOrEmpty(assignedTo)
                        ? assignedTo.Split(',').Where(x => Guid.TryParse(x, out _)).Select(Guid.Parse).ToList()
                        : new List<Guid>();

                    if (!string.IsNullOrWhiteSpace(taskTitle))
                    {
                        var task = new TaskInListModel
                        {
                            Id = Guid.NewGuid(),
                            Board_Id = boardId,
                            List_Id = currentList.Id,
                            Create_At = DateTime.UtcNow,
                            Update_At = DateTime.UtcNow,
                            Permission = "Edit",
                            Task = new TaskModel
                            {
                                Id = Guid.NewGuid(),
                                Title = taskTitle,
                                Description = taskDescription,
                                Create_At = DateTime.TryParse(taskCreateAt, out var parsedCreateAt) ? parsedCreateAt : DateTime.UtcNow,
                                Finish_At = DateTime.TryParse(taskFinishAt, out var parsedFinishAt) ? parsedFinishAt : DateTime.UtcNow.AddDays(7),
                                Status = taskStatus,
                                Position = currentList.NumberOfTaskInside + 1,
                                AssignedToList = assignedToList
                            }
                        };
                        tasks.Add(task);
                        currentList.NumberOfTaskInside++;
                    }
                }

                _context.lists.AddRange(lists);
                _context.TaskInList.AddRange(tasks);
                _context.Boards.Update(board);

                await _context.SaveChangesAsync();
                return Ok("Data imported successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("board/users")]
        public async Task<IActionResult> GetBoardUsers([FromQuery] Guid boardId)
        {
            var board = await _context.Boards
                .Include(b => b.Workspace) 
                .ThenInclude(w => w.User) 
                .Include(b => b.Collaborators) 
                .ThenInclude(c => c.User) 
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                return NotFound("Board not found.");
            }

            var owner = board.Workspace.User;

            var collaborators = board.Collaborators
                .Select(c => c.User)
                .ToList();

            var users = collaborators
                .Append(owner)
                .DistinctBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.UserName,
                    u.AvatarImage
                })
                .ToList();

            return Ok(users);
        }


    }


}