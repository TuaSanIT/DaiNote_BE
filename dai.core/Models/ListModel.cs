namespace dai.core.Models;

public class ListModel
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public DateTime Create_At { get; set; }

    public DateTime Update_At { get; set; }

    public string Status { get; set; }

    public int Position { get; set; }

    public int NumberOfTaskInside { get; set; }


    public ICollection<TaskInListModel> taskInList { get; set; }
}
