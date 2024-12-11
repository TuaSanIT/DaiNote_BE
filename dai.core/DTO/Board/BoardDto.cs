using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Board
{
    public class BoardDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid WorkspaceId { get; set; }
        public DateTime Create_At { get; set; }
        public DateTime Update_At { get; set; }
        public string Status { get; set; }
    }
}