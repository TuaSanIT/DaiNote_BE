using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dai.core.Models
{
    public class LabelModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public DateTime Created { get; set; }
        public DateTime Edited { get; set; }

        public Guid UserId { get; set; }
        public UserModel User { get; set; }

        public ICollection<NoteLabelModel> NoteLabels { get; set; }
    }
}
