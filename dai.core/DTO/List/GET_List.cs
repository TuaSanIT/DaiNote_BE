using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.List;

public class GET_List
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public DateTime Create_At { get; set; }

    public DateTime Update_At { get; set; }

    public string Status { get; set; }

    public int Position { get; set; }

    public int NumberOfTaskInside { get; set; }
}
