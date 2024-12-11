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
        public bool? Bookmark { get; set; }

        // List of image base64 strings (if you're storing them as byte arrays)
        public List<string>? Images { get; set; } = new List<string>();
    }
}