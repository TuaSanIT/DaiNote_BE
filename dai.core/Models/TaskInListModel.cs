using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace dai.core.Models;

public class TaskInListModel
{
    public Guid Id { get; set; }  

    public Guid Board_Id { get; set; }
    public BoardModel Board { get; set; }

    public Guid? Task_Id { get; set; }
    public TaskModel Task { get; set; }

    public Guid? List_Id { get; set; }
    public ListModel List { get; set; }

    public DateTime Create_At { get; set; }

    public DateTime Update_At { get; set; }

    public string Permission {  get; set; } 
}
