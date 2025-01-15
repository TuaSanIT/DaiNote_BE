using Microsoft.AspNetCore.Identity;

namespace dai.core.Models;

public class UserModel : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public DateTime AddedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? LastLoginIp { get; set; }
    public string? AvatarImage { get; set; }
    public bool? IsVipSupplier { get; set; }
    public DateTime? VipExpiryDate { get; set; }
    public string? LoginProvider { get; set; }
    public bool? IsEmailConfirm { get; set; }
    public bool? IsOnline { get; set; } = false;
    public DateTime? LastOnlineAt { get; set; }
    public string? Role { get; set; }
    public bool IsBanned { get; set; } = false;

// Add TimeZoneId property
public string? TimeZoneId { get; set; } = "UTC"; // Default to UTC

    public ICollection<WorkspaceModel> Workspace { get; set; } = new List<WorkspaceModel>();
    public ICollection<CollaboratorModel> Collaborator { get; set; } = new List<CollaboratorModel>();
    public ICollection<NoteModel> Note { get; set; } = new List<NoteModel>();
    public ICollection<LabelModel> Label { get; set; } = new List<LabelModel>();
    public ICollection<TaskModel> Task { get; set; } = new List<TaskModel>();
    public ICollection<TransactionModel> Transactions { get; set; } = new List<TransactionModel>();

    ////Message là dùng Guid
    //public virtual ICollection<Message> MessageReceivers { get; } = new List<Message>();
    //public virtual ICollection<Message> MessageSenders { get; } = new List<Message>();
}

