using dai.core.DTO.Collaborator;
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
    public class CollaboratorRepository : ICollaboratorRepository
    {
        private readonly AppDbContext _context;

        public CollaboratorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserModel> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<BoardModel> GetBoardByIdAsync(Guid boardId)
        {
            return await _context.Boards.FindAsync(boardId);
        }

        public async Task<BoardModel> GetBoardByInvitationCodeAsync(Guid invitationCode)
        {
            var collaborator = await _context.Collaborators
                .Include(c => c.Board)
                .FirstOrDefaultAsync(c => c.Invitation_Code == invitationCode);

            return collaborator?.Board;
        }

        public async Task<CollaboratorInvitationModel> GetInvitationByCodeAsync(Guid invitationCode)
        {
            return await _context.CollaboratorInvitations 
                .Include(i => i.Collaborators)
                .FirstOrDefaultAsync(i => i.Invitaion_Code == invitationCode);
        }

        public async Task<bool> IsUserCollaboratorAsync(Guid userId, Guid boardId)
        {
            return await _context.Collaborators
                .AnyAsync(c => c.User_Id == userId && c.Board_Id == boardId);
        }

        public async Task UpdateCollaboratorAsync(CollaboratorModel collaborator)
        {
            _context.Collaborators.Update(collaborator);
            await _context.SaveChangesAsync();
        }

        public async Task CreateInvitationAsync(CollaboratorInvitationModel invitation)
        {
            _context.CollaboratorInvitations .Add(invitation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateInvitationAsync(CollaboratorInvitationModel invitation)
        {
            _context.CollaboratorInvitations .Update(invitation);
            await _context.SaveChangesAsync();
        }

        public async Task<CollaboratorModel> GetCollaboratorByInvitationCodeAndUserIdAsync(Guid invitationCode, Guid userId)
        {
            return await _context.Collaborators
                .FirstOrDefaultAsync(c => c.Invitation_Code == invitationCode && c.User_Id == userId);
        }

        public async Task<CollaboratorModel> GetCollaboratorByBoardIdAndUserIdAsync(Guid boardId, Guid userId)
        {
            return await _context.Collaborators
                .FirstOrDefaultAsync(c => c.Board_Id == boardId && c.User_Id == userId);
        }

        public async Task<List<GET_UserInCollaborator>> GetCollaboratorsByBoardIdAsync(Guid boardId)
        {

            var board = await _context.Boards
                .Where(b => b.Id == boardId)
                .Include(b => b.Workspace.User) // Assuming the user who created the board is the user associated with the workspace
                .Include(b => b.Collaborators)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync();

            if (board == null)
            {
                return new List<GET_UserInCollaborator>(); // Return an empty list if the board is not found
            }


            var collaborators = board.Collaborators
                .Select(c => new GET_UserInCollaborator
                {
                    UserId = c.User_Id,
                    UserName = c.User.UserName,
                    UserEmail = c.User.Email,
                    Permission = c.Permission,
                    Image = c.User.AvatarImage
                })
                .ToList();


            var creator = new GET_UserInCollaborator
            {
                UserId = board.Workspace.User.Id,
                UserName = board.Workspace.User.UserName, // Adjust based on your UserModel
                UserEmail = board.Workspace.User.Email,
                Permission = "Owner", // You can set a specific permission for the owner if needed
                Image = board.Workspace.User.AvatarImage
            };

            if (!collaborators.Any(c => c.UserId == creator.UserId))
            {
                collaborators.Add(creator);
            }

            return collaborators;
        }

        public async Task AddOrUpdateCollaboratorAsync(CollaboratorModel collaborator)
        {
            var existingCollaborator = await _context.Collaborators
                .FirstOrDefaultAsync(c => c.User_Id == collaborator.User_Id && c.Board_Id == collaborator.Board_Id);

            if (existingCollaborator != null)
            {

                _context.Collaborators.Remove(existingCollaborator);
                await _context.SaveChangesAsync();
            }


            await _context.Collaborators.AddAsync(collaborator);
            await _context.SaveChangesAsync();
        }


        public async Task<CollaboratorModel> GetCollaboratorAsync(Guid userId, Guid boardId)
        {
            return await _context.Collaborators
                .FirstOrDefaultAsync(c => c.User_Id == userId && c.Board_Id == boardId);
        }

        public async Task<BoardModel> FindBoardWithWorkspaceAsync(Guid boardId)
        {
            return await _context.Boards
                .Include(b => b.Workspace)
                .FirstOrDefaultAsync(b => b.Id == boardId);
        }


    }
}
