using dai.api.Hubs;
using dai.core.DTO.Chat;
using dai.core.Models.Entities;
using dai.dataAccess.DbContext;
using Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatRoomController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatRoomController(AppDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
        [HttpGet("get-chat-rooms")]
        public async Task<IActionResult> GetChatRooms()
        {
            var chatRooms = await _context.ChatRooms.ToListAsync();
            return Ok(chatRooms);
        }
        [HttpDelete("delete-chat-room/{chatRoomId}")]
        public async Task<IActionResult> DeleteChatRoomAsync(Guid chatRoomId)
        {
            var deleteChatRoomById = _context.ChatRooms.FirstOrDefault(x => x.Id == chatRoomId);
            if (deleteChatRoomById != null)
            {
                _context.ChatRooms.Remove(deleteChatRoomById);
                _context.SaveChanges();
                await _hubContext.Clients.All.SendAsync("GetChatRoomSignalR", chatRoomId);

                return Ok(new { success = true });
            }
            else
            {
                return BadRequest(new { success = false, error = "Chat room not found" });
            }
        }
        [HttpPut("update-chat-room/{chatRoomId}")]
        public async Task<IActionResult> UpdateChatRoomAsync(Guid chatRoomId, string chatRoomNameToUpdate)
        {
            try
            {
                var chatRoom = _context.ChatRooms.FirstOrDefault(x => x.Id == chatRoomId);

                if (chatRoom == null)
                {
                    return NotFound();
                }

                chatRoom.Name = chatRoomNameToUpdate;
                _context.Update(chatRoom);
                _context.SaveChanges();
                await _hubContext.Clients.All.SendAsync("GetChatRoomSignalR", chatRoomId);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("create-chat-room")]
        public async Task<IActionResult> CreateRoom([FromBody] ChatRoomViewModel chatRoomViewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var chatRoom = new ChatRoom
            {
                Name = chatRoomViewModel.Name
            };

            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("GetChatRoomSignalR", chatRoom);


            return Json(new { success = true, chatRoom });
        }

        // GET: ChatRoom/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.ChatRooms == null)
            {
                return NotFound();
            }

            var chatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chatRoom == null)
            {
                return NotFound();
            }

            return View(chatRoom);
        }

        // POST: ChatRoom/Create
        [HttpPost("create")]
        public async Task<IActionResult> Create([Bind("Id,Name")] ChatRoom chatRoom)
        {
            if (ModelState.IsValid)
            {
                _context.Add(chatRoom);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(chatRoom);
        }

        // GET: ChatRoom/Edit/5
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.ChatRooms == null)
            {
                return NotFound();
            }

            var chatRoom = await _context.ChatRooms.FindAsync(id);
            if (chatRoom == null)
            {
                return NotFound();
            }
            return View(chatRoom);
        }

        // POST: ChatRoom/Edit/5
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name")] ChatRoom chatRoom)
        {
            if (id != chatRoom.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chatRoom);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChatRoomExists(chatRoom.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(chatRoom);
        }

        // GET: ChatRoom/Delete/5
        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.ChatRooms == null)
            {
                return NotFound();
            }

            var chatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chatRoom == null)
            {
                return NotFound();
            }

            return View(chatRoom);
        }

        // POST: ChatRoom/Delete/5
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.ChatRooms == null)
            {
                return Problem("Entity set 'AppDbContext.ChatRooms'  is null.");
            }
            var chatRoom = await _context.ChatRooms.FindAsync(id);
            if (chatRoom != null)
            {
                _context.ChatRooms.Remove(chatRoom);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChatRoomExists(Guid id)
        {
            return (_context.ChatRooms?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
