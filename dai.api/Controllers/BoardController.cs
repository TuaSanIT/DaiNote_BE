using AutoMapper;
using dai.core.DTO.Board;
using dai.core.Models;
using dai.dataAccess.DbContext;
using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            // Kiểm tra quyền sở hữu workspace
            var workspaceOwner = await _context.Workspaces
                .Where(w => w.Id == board.WorkspaceId)
                .Select(w => w.UserId)
                .FirstOrDefaultAsync();

            bool isOwner = workspaceOwner == userId;

            // Kiểm tra quyền Editor (collaborator)
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

                // Map the boards to DTOs
                var boardDtos = _mapper.Map<IEnumerable<BoardDto>>(boards);

                return Ok(boardDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Board/{boardId}
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

        // POST: api/board/{workspaceId}
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

        // PUT: api/Board/{boardId}
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

        // DELETE: api/Board/{boardId}
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

        // GET: api/Board/{boardId}/workspace
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

        // GET: api/Board/{boardId}/collaborators
        [HttpGet("{boardId}/collaborators")]
        public async Task<IActionResult> GetCollaborators(Guid boardId)
        {
            var currentUserId = GetUserIdFromHeader();
            if (currentUserId == null)
                return Unauthorized(new { message = "User not logged in." });

            // Lấy thông tin Board và Workspace
            var board = await _context.Boards.Include(b => b.Workspace).FirstOrDefaultAsync(b => b.Id == boardId);
            if (board == null)
                return NotFound(new { message = "Board not found." });

            // Kiểm tra nếu người dùng hiện tại là Owner của Workspace
            var isOwner = board.Workspace.UserId == currentUserId;

            // Lấy danh sách Collaborators
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

            // Thêm Owner vào danh sách nếu là Owner
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

        [HttpDelete("{boardId}/collaborator/{userId}")]
        public async Task<IActionResult> RemoveCollaborator(Guid boardId, Guid userId)
        {
            var currentUserId = GetUserIdFromHeader();
            if (currentUserId == null)
                return Unauthorized(new { message = "User not logged in." });

            // Lấy thông tin Board và Workspace
            var board = await _context.Boards.Include(b => b.Workspace).FirstOrDefaultAsync(b => b.Id == boardId);
            if (board == null)
                return NotFound(new { message = "Board not found." });

            // Kiểm tra quyền Owner
            if (board.Workspace.UserId != currentUserId)
                return StatusCode(403, new { message = "Only the Owner can remove collaborators." });

            // Xóa Collaborator
            var collaborator = await _context.Collaborators
                .FirstOrDefaultAsync(c => c.Board_Id == boardId && c.User_Id == userId);

            if (collaborator == null)
                return NotFound(new { message = "Collaborator not found." });

            _context.Collaborators.Remove(collaborator);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Collaborator removed successfully." });
        }


    }

}