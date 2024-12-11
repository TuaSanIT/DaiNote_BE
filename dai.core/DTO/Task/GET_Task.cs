using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Task;

public class GET_Task
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public DateTime Create_At { get; set; }

    public DateTime Update_At { get; set; }

    public DateTime Finish_At { get; set; }

    public string Description { get; set; }

    public string Status { get; set; }

    //public string UserEmail { get; set; }

    //public Guid UserEmailId { get; set; }

    public string FileLink { get; set; } //file name

    public int Position { get; set; }

    public bool AvailableCheck { get; set; }

    public ICollection<Guid> AssignedUsers {  get; set; }
    public Dictionary<Guid, string> AssignedUsersEmails { get; set; } // UserId -> Email mapping
}
