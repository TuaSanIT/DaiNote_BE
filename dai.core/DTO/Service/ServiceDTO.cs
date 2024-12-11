using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Service
{
    public class ServiceDTO
    {
        public int ServiceId { get; set; }
        public int SupplierId { get; set; }
        public int ServiceTypeId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Address { get; set; }
        public decimal? StarNumber { get; set; }
        public string? VisitWebsiteLink { get; set; }
        public int? IsActive { get; set; }
    }
}
