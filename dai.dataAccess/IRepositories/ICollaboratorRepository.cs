using dai.core.DTO.Collaborator;
using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories
{
    public interface ICollaboratorRepository
    {
        Task<BoardModel> GetBoardByIdAsync(Guid boardId);
        Task<BoardModel> GetBoardByInvitationCodeAsync(Guid invitationCode);
        Task<CollaboratorInvitationModel> GetInvitationByCodeAsync(Guid invitationCode);
        Task<bool> IsUserCollaboratorAsync(Guid userId, Guid boardId);
        Task UpdateCollaboratorAsync(CollaboratorModel collaborator);
        Task CreateInvitationAsync(CollaboratorInvitationModel invitation);
        Task UpdateInvitationAsync(CollaboratorInvitationModel invitation);
        Task<CollaboratorModel> GetCollaboratorByInvitationCodeAndUserIdAsync(Guid invitationCode, Guid userId);
        Task<CollaboratorModel> GetCollaboratorByBoardIdAndUserIdAsync(Guid boardId, Guid userId);
        Task<List<GET_UserInCollaborator>> GetCollaboratorsByBoardIdAsync(Guid boardId);
        Task<CollaboratorModel> GetCollaboratorAsync(Guid userId, Guid boardId);
        Task AddOrUpdateCollaboratorAsync(CollaboratorModel collaborator);
    }
}
