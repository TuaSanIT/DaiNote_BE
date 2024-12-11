using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models.Notifications
{
    [Table("Notification")]
    public partial class Notification
    {
        [Key]
        public Guid Id { get; set; }
        public Guid? UserId { get; set; } // nhớ get set dùm, tên côt collumn Chat
        [ForeignKey("UserId")] // tên sẽ lưu trong db
        public virtual UserModel? User { get; set; } // tên cột trong bảng ApplicationUser (đặt tùy ý)
        public string Message { get; set; } = null!;
        public string MessageType { get; set; } = null!;
        public DateTime NotificationDateTime { get; set; }
    }
}
