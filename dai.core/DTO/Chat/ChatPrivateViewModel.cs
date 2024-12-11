using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Chat
{
    public class ChatPrivateViewModel
    {
        public Guid ReceiverUserId { get; set; }
        public string? Message { get; set; }
    }
}
