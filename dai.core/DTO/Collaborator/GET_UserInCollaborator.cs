using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Collaborator;

public class GET_UserInCollaborator
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } 
    public string UserEmail { get; set; }
    public string Permission { get; set; }
    public string Image { get; set; }
}
