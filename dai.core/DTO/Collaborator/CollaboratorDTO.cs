using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Collaborator
{
    public class CollaboratorDTO
    {
        public Guid Board_Id { get; set; }
        public string BoardName { get; set; }
        public Guid User_Id { get; set; }
        public string UserName { get; set; }
        public string Permission { get; set; }
    }

}
