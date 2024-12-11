using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Label
{
    public class GetLabelDTO
    {
        public Guid LabelId { get; set; }  // Add this field to include LabelId
        public string Name { get; set; }
    }
}
