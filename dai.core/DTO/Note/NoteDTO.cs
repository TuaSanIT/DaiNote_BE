using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Note
{
    public class NoteDTO
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public DateTime? Reminder { get; set; }
        public bool? Bookmark { get; set; }
        public bool? Archive { get; set; }
        public bool? Trash { get; set; }
        public IFormFileCollection? Images { get; set; } // New image uploads
        public List<string>? DeletedImages { get; set; } // URLs of images to delete
    }
}