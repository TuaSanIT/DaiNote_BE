using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.DragAndDrop;

public class MoveTaskToListRequest
{
    public Guid TaskId { get; set; }
    public Guid TargetListId { get; set; }
}
