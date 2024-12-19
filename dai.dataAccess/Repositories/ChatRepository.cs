using dai.core.Models.Entities;
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
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Chat> CreateChatAsync(Chat chat)
        {
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task<List<Chat>> GetChatsByRoomIdAsync(Guid chatRoomId)
        {
            return await _context.Chats
                .Where(c => c.ChatRoomDataId == chatRoomId)
                .Include(c => c.Id)
                .OrderBy(c => c.NotificationDateTime)
                .ToListAsync();
        }

        public async Task<ChatPrivate> CreateChatPrivateAsync(ChatPrivate chatPrivate)
        {
            _context.ChatPrivate.Add(chatPrivate);
            await _context.SaveChangesAsync();
            return chatPrivate;
        }

        public async Task<List<ChatPrivate>> GetChatPrivateMessagesAsync(Guid senderId, Guid receiverId)
        {
            return await _context.ChatPrivate
                .Where(cp => (cp.SenderUserId == senderId && cp.ReceiverUserId == receiverId) ||
                             (cp.SenderUserId == receiverId && cp.ReceiverUserId == senderId))
                .Include(cp => cp.SenderUser)
                .Include(cp => cp.ReceiverUser)
                .OrderBy(cp => cp.NotificationDateTime)
                .ToListAsync();
        }
        public async Task CreateRoomAsync(ChatRoom chatRoom)
        {
            if (chatRoom == null)
            {
                throw new ArgumentNullException(nameof(chatRoom));
            }

            await _context.ChatRooms.AddAsync(chatRoom);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatRoom>> GetAllRoomsAsync()
        {
            return await _context.ChatRooms
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ChatRoom> GetRoomByBoardIdAsync(Guid boardId)
        {
            var chatRoomUser = await _context.ChatRoomUsers
                .Include(cru => cru.ChatRoom)
                .FirstOrDefaultAsync(cru => cru.UserId == boardId);

            return chatRoomUser?.ChatRoom;
        }

    }
}
