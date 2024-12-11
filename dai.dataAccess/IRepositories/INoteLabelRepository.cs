using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories
{
    public interface INoteLabelRepository
    {
        Task AddNoteLabelsAsync(IEnumerable<NoteLabelModel> noteLabels);
        Task DeleteNoteLabelsAsync(IEnumerable<NoteLabelModel> noteLabels);
        Task<NoteLabelModel> GetNoteLabelAsync(Guid noteId, Guid labelId);
        Task<IEnumerable<LabelModel>> GetLabelsByNoteIdAsync(Guid noteId);
        Task<IEnumerable<NoteModel>> GetNotesByLabelIdAsync(Guid labelId);
    }
}
