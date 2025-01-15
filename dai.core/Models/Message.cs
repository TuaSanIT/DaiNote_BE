using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        public Guid SenderId { get; set; }

        public Guid ReceiverId { get; set; }

        public string? Content { get; set; }

        public DateTime SendDate { get; set; }

        public Guid ConversationId { get; set; } 

        public virtual UserModel Receiver { get; set; } = null!;
        public virtual UserModel Sender { get; set; } = null!;
    }

}
