using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models.Entities
{
    public class ChatPrivate
    {
        [Key]
        public Guid ChatPrivateId { get; set; }

        public Guid? SenderUserId { get; set; } // Id của người gửi
        [ForeignKey("SenderUserId")]
        public virtual UserModel? SenderUser { get; set; }

        public Guid? ReceiverUserId { get; set; } // Id của người nhận
        [ForeignKey("ReceiverUserId")]
        public virtual UserModel? ReceiverUser { get; set; }

        public string Message { get; set; } = null!;
        public string? ImageChat { get; set; } = null;

        public DateTime NotificationDateTime { get; set; }

    }
}
