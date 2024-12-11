using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models.Notifications
{
    [Table("HubConnection")]
    public partial class HubConnection
    {
        [Key]
        public Guid Id { get; set; }
        public string ConnectionId { get; set; } = null!;
        public string Username { get; set; } = null!;
    }
}
