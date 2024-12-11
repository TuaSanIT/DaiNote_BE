using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.List;

public class POST_List
{
    [Required(ErrorMessage = "Title is required.")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; }
}
