﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Task;

public class PUT_Task
{
    public string Title { get; set; }

    public DateTime Finish_At { get; set; }

    public string Description { get; set; }

    public string Status { get; set; }

    public Guid? AssignTo { get; set; }

    public bool AvailableCheck { get; set; }

    public IFormFile? File { get; set; } // for add file
}

