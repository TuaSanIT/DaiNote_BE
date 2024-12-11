using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models;

public class CollaboratorModel
{
    
    public Guid Board_Id { get; set; }
    public BoardModel Board { get; set; }

    public Guid User_Id { get; set; }   
    public UserModel User { get; set; }

    public Guid Invitation_Code { get; set; }
    public CollaboratorInvitationModel CollaboratorInvitation { get; set; }

    public string Permission {  get; set; } 
}
