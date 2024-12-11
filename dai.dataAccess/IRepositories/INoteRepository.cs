using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories
{
    public interface INoteRepository
    {
        Task<IEnumerable<NoteModel>> GetNotesAsync();
        Task<IEnumerable<NoteModel>> GetNotesWithUserIdAsync(Guid userId);
        Task<NoteModel> GetNoteByIdAsync(Guid id);
        Task<NoteModel> AddBookMark(NoteModel note);
        Task<NoteModel> AddNoteAsync(Guid userId, NoteModel note);
        Task<NoteModel> UpdateNoteAsync(NoteModel note);
        Task DeleteNoteAsync(Guid id);
        Task AddNoteLabelAsync(NoteLabelModel noteLabel);
        Task RemoveNoteLabelAsync(NoteLabelModel noteLabel);
    }
}
