using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models;

public class CollaboratorInvitationModel
{
    [Key]
    public Guid Invitaion_Code { get; set; }
    public ICollection<string> Emails { get; set; }

    public Guid SenderUserId { get; set; }
    public string Status { get; set; }

    public ICollection<CollaboratorModel> Collaborators { get; set; }
}
