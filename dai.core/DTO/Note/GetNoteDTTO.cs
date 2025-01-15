using dai.core.DTO.Label;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Note
{
    public class GetNoteDTO
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
        public Guid UserId { get; set; }

        // Use List<byte[]> as per NoteModel
        public List<string>? Images { get; set; }
    }
}
