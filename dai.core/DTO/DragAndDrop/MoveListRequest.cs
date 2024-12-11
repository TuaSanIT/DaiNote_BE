using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.DragAndDrop;

public class MoveListRequest
{
    [Required(ErrorMessage = "Id for DraggedListId is required.")]
    public Guid DraggedListId { get; set; }
    [Required(ErrorMessage = "Id for TargetListId is required.")]
    public Guid TargetListId { get; set; }
}

