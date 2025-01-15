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

    public string? FileName { get; set; } // For stroring file name

    public int Position { get; set; }

    public Guid? AssignTo { get; set; }
    public UserModel User { get; set; }

    public ICollection<TaskInListModel> taskInList { get; set; }
}
