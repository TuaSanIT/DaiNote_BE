using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Collaborator
{
    public class CollaboratorInvitationDTO
    {
        public Guid BoardId { get; set; }
        public List<string> Emails { get; set; }

        public Guid SenderUserId { get; set; }
    }
}
