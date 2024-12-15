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
    public class LabelRepository : ILabelRepository
    {
        private readonly AppDbContext _context;

        public LabelRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LabelModel>> GetLabelsAsync()
        {
            return await _context.Labels.ToListAsync();
        }

        public async Task<IEnumerable<LabelModel>> GetLabelsByUserIdAsync(Guid userId)
        {
            return await _context.Labels
                .Where(l => l.UserId == userId)
                .ToListAsync();
        }

        public async Task<LabelModel> GetLabelByIdAsync(Guid id)
        {
            return await _context.Labels.FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<LabelModel> AddLabelAsync(Guid userId, LabelModel label) // Updated
        {
            label.UserId = userId; // This should already be set by the controller
            _context.Labels.Add(label);
            await _context.SaveChangesAsync();
            return label;
        }

        public async Task<LabelModel> UpdateLabelAsync(LabelModel label)
        {
            _context.Labels.Update(label);
            await _context.SaveChangesAsync();
            return label;
        }

        public async Task DeleteLabelAsync(Guid id)
        {
            try
            {
                var label = await GetLabelByIdAsync(id);
                if (label != null)
                {
                    _context.Labels.Remove(label);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error deleting label: {ex.Message}");
                throw; // Optionally rethrow
            }
        }

        public async Task AddLabelToNoteAsync(NoteLabelModel noteLabel)
        {
            _context.NoteLabels.Add(noteLabel);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveLabelFromNoteAsync(NoteLabelModel noteLabel)
        {
            _context.NoteLabels.Remove(noteLabel);
            await _context.SaveChangesAsync();
        }
    }
}
