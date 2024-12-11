using dai.core.DTO.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Workspace
{
    public class WorkspaceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; }
        public DateTime Create_At { get; set; }
        public DateTime Update_At { get; set; }

        public IEnumerable<BoardDto> Board { get; set; }
    }
}