using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories;

public interface IListRepository
{
    Task<IEnumerable<ListModel>> GetAllListsAsync();
    Task<ListModel> GetListByIdAsync(Guid id);
    Task<ListModel> AddListAsync(ListModel list, Guid boardId);
    Task<ListModel> UpdateListAsync(ListModel list);
    Task DeleteListAsync(Guid id);

    Task<IEnumerable<ListModel>> GetListsByBoardIdAsync(Guid boardId);
    Task<IEnumerable<ListModel>> GetListsAndTasksByBoardIdAsync(Guid boardId);
}
