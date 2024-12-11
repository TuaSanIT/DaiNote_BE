using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.DragAndDrop;

public class MoveTaskToAnotherListRequest
{
    [Required(ErrorMessage = "Id for DraggedTaskId is required.")]
    public Guid DraggedTaskId { get; set; }

    [Required(ErrorMessage = "Id for TargetTaskId is required.")]
    public Guid TargetTaskId { get; set; }
}

