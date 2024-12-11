using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories
{
    public interface ILabelRepository
    {
        Task<IEnumerable<LabelModel>> GetLabelsAsync();
        Task<IEnumerable<LabelModel>> GetLabelsByUserIdAsync(Guid userId);
        Task<LabelModel> GetLabelByIdAsync(Guid id);
        Task<LabelModel> AddLabelAsync(Guid userId, LabelModel label); // Updated
        Task<LabelModel> UpdateLabelAsync(LabelModel label);
        Task DeleteLabelAsync(Guid id);

        Task AddLabelToNoteAsync(NoteLabelModel noteLabel);
        Task RemoveLabelFromNoteAsync(NoteLabelModel noteLabel);
    }
}
