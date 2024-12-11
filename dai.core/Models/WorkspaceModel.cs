using System.ComponentModel.DataAnnotations.Schema;

namespace dai.core.Models;

public class WorkspaceModel
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public DateTime Create_At { get; set; }

    public DateTime Update_At { get; set; }

    public string Status { get; set; }
    public Guid UserId { get; set; }
    public UserModel User { get; set; }


    public ICollection<BoardModel> Board { get; set; }
}
