using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models.Entities
{
    public class ChatRoomUser
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ChatRoomId { get; set; }

        [ForeignKey("ChatRoomId")]
        public virtual ChatRoom ChatRoom { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual UserModel User { get; set; }
    }
}
