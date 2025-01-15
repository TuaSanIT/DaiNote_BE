using AutoMapper;
using dai.core.DTO.Workspace;
using dai.core.Models;
using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IMapper _mapper;

        public WorkspaceController(IWorkspaceRepository workspaceRepository, IMapper mapper)
        {
            _workspaceRepository = workspaceRepository;
            _mapper = mapper;
        }

        private Guid? GetUserIdFromHeader()
        {
            if (Request.Headers.TryGetValue("UserId", out var userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }

            return null;
        }

        // GET: api/workspace/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWorkspace(Guid id)
        {
            var userId = GetUserIdFromHeader();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not logged in." });
            }

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(id);
            if (workspace == null)
            {
                return NotFound(new { message = "Workspace not found." });
            }

            // Xác minh quyền sở hữu
            if (workspace.UserId != userId)
            {
                return StatusCode(403, new { message = "You do not have permission to access this workspace." });
            }

            var workspaceDto = _mapper.Map<WorkspaceDto>(workspace);
            return Ok(workspaceDto);
        }

        // POST: api/workspace
        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto createWorkspaceDto)
        {
            var userId = GetUserIdFromHeader();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not logged in." });
            }

            var workspaceModel = _mapper.Map<WorkspaceModel>(createWorkspaceDto);
            workspaceModel.UserId = userId.Value;

            try
            {
                var createdWorkspace = await _workspaceRepository.CreateWorkspaceAsync(workspaceModel);
                var workspaceDto = _mapper.Map<WorkspaceDto>(createdWorkspace);
                return CreatedAtAction(nameof(GetWorkspace), new { id = workspaceDto.Id }, workspaceDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/workspace/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] UpdateWorkspaceDto updateWorkspaceDto)
        {
            var userId = GetUserIdFromHeader();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not logged in." });
            }

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(id);
            if (workspace == null)
            {
                return NotFound(new { message = "Workspace not found." });
            }

            // Xác minh quyền sở hữu
            if (workspace.UserId != userId)
            {
                return StatusCode(403, new { message = "You do not have permission to access this workspace." });
            }

            _mapper.Map(updateWorkspaceDto, workspace);
            workspace.Status = updateWorkspaceDto.Status ?? workspace.Status;

            await _workspaceRepository.UpdateWorkspaceAsync(workspace);
            return NoContent();
        }

        // DELETE: api/workspace/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkspace(Guid id)
        {
            var userId = GetUserIdFromHeader();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not logged in." });
            }

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(id);
            if (workspace == null)
            {
                return NotFound(new { message = "Workspace not found." });
            }

            // Xác minh quyền sở hữu
            if (workspace.UserId != userId)
            {
                return StatusCode(403, new { message = "You do not have permission to access this workspace." });
            }

            await _workspaceRepository.DeleteWorkspaceAsync(id);
            return NoContent();
        }


        // GET: api/workspace/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetWorkspacesByUserId(Guid userId)
        {
            var workspaces = await _workspaceRepository.GetWorkspacesByUserIdAsync(userId);
            if (workspaces == null || !workspaces.Any())
            {
                return Ok(new List<WorkspaceDto>());
            }

            var workspaceDtos = _mapper.Map<IEnumerable<WorkspaceDto>>(workspaces);
            return Ok(workspaceDtos);
        }

    }

}