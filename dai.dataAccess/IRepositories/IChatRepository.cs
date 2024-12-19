using dai.core.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories
{
    public interface IChatRepository
    {
        Task<Chat> CreateChatAsync(Chat chat);
        Task<List<Chat>> GetChatsByRoomIdAsync(Guid chatRoomId);
        Task<ChatPrivate> CreateChatPrivateAsync(ChatPrivate chatPrivate);
        Task<List<ChatPrivate>> GetChatPrivateMessagesAsync(Guid senderId, Guid receiverId);
        Task CreateRoomAsync(ChatRoom chatRoom);
        Task<List<ChatRoom>> GetAllRoomsAsync();
        Task<ChatRoom> GetRoomByBoardIdAsync(Guid boardId);
    }

}
