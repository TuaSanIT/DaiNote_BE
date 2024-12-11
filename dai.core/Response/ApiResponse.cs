using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Response
{
    public class ApiResponse
    {
        public bool? Status { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }
}
