using dai.core.DTO.Label;
using dai.core.DTO.Note;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.NoteLabel
{
    public class NoteLabelDTO
    {
        public Guid LabelNoteID { get; set; }
        public Guid LabelID { get; set; }
        public LabelDTO Label { get; set; }
        public Guid NoteID { get; set; }
        public NoteDTO Note { get; set; }
    }
}
