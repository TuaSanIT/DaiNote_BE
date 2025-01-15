using System.ComponentModel.DataAnnotations.Schema;

namespace dai.core.Models;

public class BoardModel
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public DateTime Create_At { get; set; }

    public DateTime Update_At { get; set; }

    public string Status { get; set; }

    public Guid WorkspaceId { get; set; }
    public WorkspaceModel Workspace { get; set; }

    public int NumberOfListInside { get; set; }


    public ICollection<TaskInListModel> taskInList { get; set; }
    public ICollection<CollaboratorModel> Collaborators { get; set; }
}
