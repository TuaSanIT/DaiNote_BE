using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Chat
{
    public class ChatViewModel
    {
        public string? Message { get; set; }

        public Guid ChatRoomId { get; set; }
    }
}
