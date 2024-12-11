using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories
{
    public interface IWorkspaceRepository
    {
        Task<IEnumerable<WorkspaceModel>> GetAllWorkspacesAsync();
        Task<WorkspaceModel> GetWorkspaceByIdAsync(Guid workspaceId);
        Task<WorkspaceModel> CreateWorkspaceAsync(WorkspaceModel workspace);
        Task UpdateWorkspaceAsync(WorkspaceModel workspace);
        Task DeleteWorkspaceAsync(Guid workspaceId);

        Task<IEnumerable<WorkspaceModel>> GetWorkspacesByUserIdAsync(Guid userId);

        // Kiểm tra quyền sở hữu
        Task<bool> IsUserOwnerOfWorkspaceAsync(Guid userId, Guid workspaceId);
    }

}