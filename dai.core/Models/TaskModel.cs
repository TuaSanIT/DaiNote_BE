using System.ComponentModel.DataAnnotations.Schema;

namespace dai.core.Models;

public class TaskModel
{
    public Guid Id { get; set; }
    
    public string Title { get; set; }

    public DateTime Create_At { get; set; }

    public DateTime Update_At { get; set; }

    public DateTime Finish_At { get; set; }

    public string Description { get; set; }

    public string Status { get; set; }

    public bool AvailableCheck { get; set; }

    public string? FileName { get; set; } 

    public int Position { get; set; }





    public string AssignedTo { get; set; }


    [NotMapped]
    public List<Guid> AssignedToList
    {
        get => string.IsNullOrEmpty(AssignedTo)
                ? new List<Guid>()
                : AssignedTo.Split(',').Select(Guid.Parse).ToList();
        set => AssignedTo = string.Join(",", value);
    }

    public ICollection<TaskInListModel> taskInList { get; set; }
}
