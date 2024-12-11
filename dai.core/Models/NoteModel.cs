using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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
        public List<byte[]>? Images { get; set; } = new List<byte[]>();
        public Guid UserId { get; set; }
        public UserModel User { get; set; }

        public ICollection<NoteLabelModel> NoteLabels { get; set; }
    }
}
