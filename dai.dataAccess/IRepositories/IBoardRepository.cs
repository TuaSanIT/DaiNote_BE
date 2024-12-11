using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories
{
    public interface IBoardRepository
    {
        Task<IEnumerable<BoardModel>> GetAllBoardsAsync();
        Task<BoardModel> GetBoardByIdAsync(Guid boardId);
        Task<BoardModel> CreateBoardAsync(BoardModel board);
        Task UpdateBoardAsync(BoardModel board);
        Task DeleteBoardAsync(Guid boardId);
        Task<Guid?> GetWorkspaceIdByBoardIdAsync(Guid boardId);
        Task<bool> IsBoardOwnerAsync(Guid userId, Guid boardId);
        Task<bool> IsBoardCollaboratorAsync(Guid userId, Guid boardId);
        Task<IEnumerable<UserModel>> GetBoardParticipantsAsync(Guid boardId);
    }
}
