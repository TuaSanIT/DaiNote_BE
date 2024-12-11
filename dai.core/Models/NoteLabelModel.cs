using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace dai.core.Models
{
    public class NoteLabelModel
    {
        public Guid NoteId { get; set; }
        public NoteModel Note { get; set; }

        public Guid LabelId { get; set; }
        public LabelModel Label { get; set; }
    }
}
