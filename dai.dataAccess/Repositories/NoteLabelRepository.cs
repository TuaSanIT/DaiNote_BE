using dai.core.Models;
using dai.dataAccess.DbContext;
using dai.dataAccess.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.Repositories
{
    public class NoteLabelRepository : INoteLabelRepository
    {
        private readonly AppDbContext _context;

        public NoteLabelRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddNoteLabelsAsync(IEnumerable<NoteLabelModel> noteLabels)
        {
            _context.NoteLabels.AddRange(noteLabels);
            await _context.SaveChangesAsync();
        }

        public async Task<NoteLabelModel> GetNoteLabelAsync(Guid noteId, Guid labelId)
        {
            return await _context.NoteLabels
                .Include(nl => nl.Label)
                .Include(nl => nl.Note)
                .FirstOrDefaultAsync(nl => nl.NoteId == noteId && nl.LabelId == labelId);
        }


        public async Task<IEnumerable<LabelModel>> GetLabelsByNoteIdAsync(Guid noteId)
        {
            return await _context.NoteLabels
                .Where(nl => nl.NoteId == noteId)
                .Include(nl => nl.Label) // Include the related Label
                .Select(nl => nl.Label)
                .ToListAsync();
        }

        public async Task<IEnumerable<NoteModel>> GetNotesByLabelIdAsync(Guid labelId)
        {
            return await _context.NoteLabels
                                 .Where(nl => nl.LabelId == labelId)
                                 .Select(nl => nl.Note)
                                 .ToListAsync();
        }

        public async Task DeleteNoteLabelsAsync(IEnumerable<NoteLabelModel> noteLabels)
        {
            _context.NoteLabels.RemoveRange(noteLabels);
            await _context.SaveChangesAsync();
        }
    }
}