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
    public class NoteRepository : INoteRepository
    {
        private readonly AppDbContext _context;

        public NoteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NoteModel>> GetNotesAsync()
        {
            return await _context.Notes
                .Include(n => n.NoteLabels)
                .ThenInclude(nl => nl.Label)
                .ToListAsync();
        }

        public async Task<IEnumerable<NoteModel>> GetNotesWithUserIdAsync(Guid userId) // Updated
        {
            return await _context.Notes
                .Where(n => n.UserId == userId) // Filter by userId
                .Include(n => n.NoteLabels)
                .ThenInclude(nl => nl.Label)
                .ToListAsync();
        }

        public async Task<NoteModel> GetNoteByIdAsync(Guid id)
        {
            return await _context.Notes
                .Include(n => n.NoteLabels)
                .ThenInclude(nl => nl.Label)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<NoteModel> AddBookMark(NoteModel note)
        {
            _context.Notes.Update(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<NoteModel> AddNoteAsync(Guid userId, NoteModel note) // Updated
        {
            note.UserId = userId;  // Assign the userId
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<NoteModel> UpdateNoteAsync(NoteModel note)
        {
            _context.Notes.Update(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task DeleteNoteAsync(Guid id)
        {
            var note = await GetNoteByIdAsync(id);
            if (note != null)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddNoteLabelAsync(NoteLabelModel noteLabel)
        {
            _context.NoteLabels.Add(noteLabel);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveNoteLabelAsync(NoteLabelModel noteLabel)
        {
            _context.NoteLabels.Remove(noteLabel);
            await _context.SaveChangesAsync();
        }
    }
}