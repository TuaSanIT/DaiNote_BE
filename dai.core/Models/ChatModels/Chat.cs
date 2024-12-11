using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models.Entities
{
    public partial class Chat
    {
        [Key]
        public Guid ChatId { get; set; }
        public Guid? UserId { get; set; } // nhớ get set dùm, tên côt collumn Chat
        [ForeignKey("UserId")] // tên sẽ lưu trong db
        public virtual UserModel? Id { get; set; } // tên cột trong bảng UserModel (đặt tùy ý)

        public string Message { get; set; } = null!;
        public string MessageType { get; set; } = null!;
        public DateTime NotificationDateTime { get; set; }
        public string Avatar { get; set; } = null!;

        public Guid? ChatRoomDataId { get; set; }
        [ForeignKey("ChatRoomDataId")]
        public virtual ChatRoom ChatRoomData { get; set; }
        public string? ImageChatRoom { get; set; } = null; // cho phép null và giá trị mặc định là null

    }
}
