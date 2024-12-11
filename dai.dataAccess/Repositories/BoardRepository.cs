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
    public class BoardRepository : IBoardRepository
    {
        private readonly AppDbContext _context;

        public BoardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BoardModel>> GetAllBoardsAsync()
        {
            return await _context.Boards.ToListAsync();
        }

        public async Task<BoardModel> GetBoardByIdAsync(Guid boardId)
        {
            return await _context.Boards
                .Include(b => b.Workspace)
                .Include(b => b.taskInList)
                .Include(b => b.Collaborators)
                .FirstOrDefaultAsync(b => b.Id == boardId);
        }

        public async Task<BoardModel> CreateBoardAsync(BoardModel board)
        {
            var workspace = await _context.Workspaces.FindAsync(board.WorkspaceId);
            if (workspace == null)
            {
                throw new Exception("Workspace not found");
            }

            board.Create_At = DateTime.UtcNow;
            board.Update_At = DateTime.UtcNow;
            board.NumberOfListInside = 0;
            _context.Boards.Add(board);
            await _context.SaveChangesAsync();
            return board;
        }

        public async Task UpdateBoardAsync(BoardModel board)
        {
            board.Update_At = DateTime.UtcNow; 
            _context.Boards.Update(board);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBoardAsync(Guid boardId)
        {
            var board = await _context.Boards
                .Include(b => b.taskInList)
                .ThenInclude(t => t.Task)  
                .Include(b => b.taskInList)
                .ThenInclude(t => t.List)  
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board != null)
            {
               
                foreach (var taskInList in board.taskInList)
                {
                    if (taskInList.Task != null)
                    {
                        _context.Tasks.Remove(taskInList.Task); 
                    }
                    if (taskInList.List != null)
                    {
                        _context.lists.Remove(taskInList.List); 
                    }
                }

               
                _context.Boards.Remove(board);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Board not found");
            }
        }

        public async Task<Guid?> GetWorkspaceIdByBoardIdAsync(Guid boardId)
        {
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
            return board?.WorkspaceId;
        }

        public async Task<bool> IsBoardOwnerAsync(Guid userId, Guid boardId)
        {
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
            return board != null && board.Workspace.UserId == userId;
        }

        public async Task<bool> IsBoardCollaboratorAsync(Guid userId, Guid boardId)
        {
            return await _context.Collaborators.AnyAsync(c => c.Board_Id == boardId && c.User_Id == userId);
        }

        public async Task<IEnumerable<UserModel>> GetBoardParticipantsAsync(Guid boardId)
        {
            var owner = await _context.Boards
                .Where(b => b.Id == boardId)
                .Select(b => b.Workspace.User)
                .FirstOrDefaultAsync();

            var collaborators = await _context.Collaborators
                .Where(c => c.Board_Id == boardId)
                .Select(c => c.User)
                .ToListAsync();

            var participants = new List<UserModel>();
            if (owner != null) participants.Add(owner);
            participants.AddRange(collaborators);

            return participants.Distinct().ToList();
        }
    }

}
