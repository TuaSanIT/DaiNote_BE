using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Board
{
    public class CreateBoardDto
    {
        public string Name { get; set; }
        public string Status { get; set; } = "Active";
    }
}