using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dai.core.Models
{
    public class NoteModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? Reminder { get; set; }
        public DateTime Created { get; set; }
        public DateTime Edited { get; set; }
        public string? Status { get; set; }
        public bool? Bookmark { get; set; }
        public string? Color { get; set; }
        public bool? Archive { get; set; }
        public bool? Trash { get; set; }
        public DateTime TrashDate { get; set; }
        public bool? TrashIsNotified { get; set; }
        public bool ReminderSent { get; set; }
        public List<string>? Images { get; set; } = new List<string>();
        public Guid UserId { get; set; }
        public UserModel User { get; set; }

        [JsonIgnore]
        public ICollection<NoteLabelModel> NoteLabels { get; set; }
    }
}
