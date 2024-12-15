using dai.core.Models;
using dai.dataAccess.DbContext;
using dai.dataAccess.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.Repositories
{
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly AppDbContext _context;

        public WorkspaceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WorkspaceModel>> GetAllWorkspacesAsync()
        {
            return await _context.Workspaces
                .Include(w => w.Board)
                    .ThenInclude(b => b.taskInList)
                        .ThenInclude(t => t.Task)
                .Include(w => w.Board)
                    .ThenInclude(b => b.taskInList)
                        .ThenInclude(t => t.List)
                .ToListAsync();
        }


        public async Task<WorkspaceModel> GetWorkspaceByIdAsync(Guid workspaceId)
        {
            return await _context.Workspaces
                .Include(w => w.Board)
                .ThenInclude(b => b.taskInList)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);
        }

        public async Task<WorkspaceModel> CreateWorkspaceAsync(WorkspaceModel workspace)
        {
            workspace.Create_At = DateTime.UtcNow;
            workspace.Update_At = DateTime.UtcNow;
            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();
            return workspace;
        }

        public async Task UpdateWorkspaceAsync(WorkspaceModel workspace)
        {
            workspace.Update_At = DateTime.UtcNow;
            _context.Workspaces.Update(workspace);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteWorkspaceAsync(Guid workspaceId)
        {

            var workspace = await _context.Workspaces
                .Include(w => w.Board)
                    .ThenInclude(b => b.taskInList)
                        .ThenInclude(t => t.Task)
                .Include(w => w.Board)
                    .ThenInclude(b => b.taskInList)
                        .ThenInclude(t => t.List)
                .Include(w => w.Board)
                    .ThenInclude(b => b.Collaborators) // Tải Collaborators trong Board
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null)
            {
                throw new KeyNotFoundException("Workspace not found");
            }


            var collaboratorsToRemove = workspace.Board
                .SelectMany(b => b.Collaborators);
            _context.Collaborators.RemoveRange(collaboratorsToRemove);


            var tasksToRemove = workspace.Board
                .SelectMany(b => b.taskInList)
                .Where(tl => tl.Task != null)
                .Select(tl => tl.Task);
            _context.Tasks.RemoveRange(tasksToRemove);

            var listsToRemove = workspace.Board
                .SelectMany(b => b.taskInList)
                .Where(tl => tl.List != null)
                .Select(tl => tl.List);
            _context.lists.RemoveRange(listsToRemove);


            _context.Boards.RemoveRange(workspace.Board);


            _context.Workspaces.Remove(workspace);


            await _context.SaveChangesAsync();
        }


        public async Task<IEnumerable<WorkspaceModel>> GetWorkspacesByUserIdAsync(Guid userId)
        {
            return await _context.Workspaces
                .Where(w => w.UserId == userId) // Assuming WorkspaceModel has a UserId property
                .Include(w => w.Board)
                    .ThenInclude(b => b.taskInList)
                        .ThenInclude(t => t.Task)
                .Include(w => w.Board)
                    .ThenInclude(b => b.taskInList)
                        .ThenInclude(t => t.List)
                .ToListAsync();
        }

        public async Task<bool> IsUserOwnerOfWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            return await _context.Workspaces.AnyAsync(w => w.Id == workspaceId && w.UserId == userId);
        }

    }

}