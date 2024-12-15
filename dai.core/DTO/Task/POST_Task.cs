using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Task;

public class POST_Task
{
    [Required(ErrorMessage = "Title can not be null")]
    public string Title { get; set; }

    public DateTime Create_At { get; set; }

    public DateTime Finish_At { get; set; }

    [Required(ErrorMessage = "Description can not be null")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Status can not be null")]
    public string Status { get; set; }



    public bool AvailableCheck { get; set; }

}
