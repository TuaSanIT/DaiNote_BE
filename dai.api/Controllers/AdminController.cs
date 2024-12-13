using dai.dataAccess.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Get User Statistics
        [HttpGet("user-statistics")]
        public async Task<IActionResult> GetUserStatistics()
        {
            var totalUsers = await _context.Users.AsNoTracking().CountAsync();
            var activeUsers = await _context.Users.AsNoTracking().Where(u => u.IsOnline == true).CountAsync();
            var vipUsers = await _context.Users.AsNoTracking().Where(u => u.IsVipSupplier == true).CountAsync();

            return Ok(new
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                VipUsers = vipUsers
            });
        }

        // Get All Users
        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role != "Admin") // Lọc bỏ các user có vai trò là Admin
                .Select(u => new
                {
                    u.Id,
                    u.AvatarImage,
                    u.UserName,
                    u.Email,
                    u.FullName,
                    AddedOn = u.AddedOn.ToString("yyyy-MM-dd"),
                    u.IsOnline,
                    u.IsVipSupplier
                })
                .ToListAsync();

            return Ok(new
            {
                TotalUsers = users.Count,
                Users = users
            });
        }

        // Get All Notes
        [HttpGet("get-all-notes")]
        public async Task<IActionResult> GetAllNotes()
        {
            var notes = await _context.Notes.AsNoTracking().Select(n => new
            {
                n.Id,
                n.Title,
                n.Description,
                Created = n.Created.ToString("yyyy-MM-dd"),
                n.Status,
                n.Bookmark,
                n.Color,
                n.Archive
            }).ToListAsync();

            return Ok(new
            {
                TotalNotes = notes.Count,
                Notes = notes
            });
        }

        // Get All Tasks
        [HttpGet("get-all-tasks")]
        public async Task<IActionResult> GetAllTasks()
        {
            var tasks = await _context.Tasks.AsNoTracking().Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                CreatedAt = t.Create_At.ToString("yyyy-MM-dd"),
                UpdatedAt = t.Update_At.ToString("yyyy-MM-dd"),
                FinishedAt = t.Finish_At.ToString("yyyy-MM-dd"),
                t.Status,
                t.AvailableCheck,
                t.FileName,
                t.Position,
                //AssignedTo = t.AssignTo
            }).ToListAsync();

            return Ok(new
            {
                TotalTasks = tasks.Count,
                Tasks = tasks
            });
        }

        [HttpGet("tasks-monthly-growth")]
        public async Task<IActionResult> GetTasksMonthlyGrowth()
        {
            var now = DateTime.UtcNow;

            // Xác định khoảng thời gian của tháng hiện tại
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddSeconds(-1);

            // Xác định khoảng thời gian của tháng trước
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            var previousMonthEnd = currentMonthStart.AddSeconds(-1);

            // Đếm số lượng task được tạo trong tháng hiện tại
            var currentMonthTaskCount = await _context.Tasks
                .Where(t => t.Create_At >= currentMonthStart && t.Create_At <= currentMonthEnd)
                .CountAsync();

            // Đếm số lượng task được tạo trong tháng trước
            var previousMonthTaskCount = await _context.Tasks
                .Where(t => t.Create_At >= previousMonthStart && t.Create_At <= previousMonthEnd)
                .CountAsync();

            // Tính tỷ lệ tăng trưởng (%)
            double growthPercentage = 0;
            if (previousMonthTaskCount > 0)
            {
                growthPercentage = ((double)(currentMonthTaskCount - previousMonthTaskCount) / previousMonthTaskCount) * 100;
            }
            else if (currentMonthTaskCount > 0)
            {
                growthPercentage = 100;
            }

            return Ok(new
            {
                CurrentMonthTasks = currentMonthTaskCount,
                PreviousMonthTasks = previousMonthTaskCount,
                GrowthPercentage = growthPercentage
            });
        }

        [HttpGet("notes-monthly-growth")]
        public async Task<IActionResult> GetNotesMonthlyGrowth()
        {
            var now = DateTime.UtcNow;

            // Xác định khoảng thời gian của tháng hiện tại
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddSeconds(-1);

            // Xác định khoảng thời gian của tháng trước
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            var previousMonthEnd = currentMonthStart.AddSeconds(-1);

            // Đếm số lượng note được tạo trong tháng hiện tại
            var currentMonthNoteCount = await _context.Notes
                .Where(n => n.Created >= currentMonthStart && n.Created <= currentMonthEnd)
                .CountAsync();

            // Đếm số lượng note được tạo trong tháng trước
            var previousMonthNoteCount = await _context.Notes
                .Where(n => n.Created >= previousMonthStart && n.Created <= previousMonthEnd)
                .CountAsync();

            // Tính tỷ lệ tăng trưởng (%)
            double growthPercentage = 0;
            if (previousMonthNoteCount > 0)
            {
                growthPercentage = ((double)(currentMonthNoteCount - previousMonthNoteCount) / previousMonthNoteCount) * 100;
            }
            else if (currentMonthNoteCount > 0)
            {
                growthPercentage = 100;
            }

            return Ok(new
            {
                CurrentMonthNotes = currentMonthNoteCount,
                PreviousMonthNotes = previousMonthNoteCount,
                GrowthPercentage = growthPercentage
            });
        }

        [HttpGet("get-all-transactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            var transactions = await _context.Transactions.AsNoTracking().Select(t => new
            {
                t.Id,
                t.OrderCode,
                t.Status,
                t.Amount,
                t.Description,
                CreatedAt = t.CreatedAt.ToString("yyyy-MM-dd"),
                PaidAt = t.PaidAt.HasValue ? t.PaidAt.Value.ToString("yyyy-MM-dd") : null,
                t.UserId,
            }).ToListAsync();

            // Lọc các giao dịch có trạng thái PAID
            var paidTransactions = transactions.Where(t => t.Status == "PAID").ToList();

            // Tính tổng số tiền từ các giao dịch PAID
            var totalPaidAmount = paidTransactions.Sum(t => t.Amount);

            return Ok(new
            {
                TotalTransactions = transactions.Count,
                TotalPaidAmount = totalPaidAmount, // Chỉ tổng tiền từ giao dịch PAID
                Transactions = transactions
            });
        }

        [HttpGet("get-filtered-transactions")]
        public async Task<IActionResult> GetFilteredTransactions(DateTime? createdFrom, DateTime? createdTo, DateTime? paidFrom, DateTime? paidTo)
        {
            var query = _context.Transactions
                .Include(t => t.User) // Bao gồm thông tin User
                .AsNoTracking();

            if (createdFrom.HasValue)
                query = query.Where(t => t.CreatedAt >= createdFrom.Value);

            if (createdTo.HasValue)
                query = query.Where(t => t.CreatedAt <= createdTo.Value);

            if (paidFrom.HasValue)
                query = query.Where(t => t.PaidAt >= paidFrom.Value);

            if (paidTo.HasValue)
                query = query.Where(t => t.PaidAt <= paidTo.Value);

            var transactions = await query.Select(t => new
            {
                t.Id,
                t.OrderCode,
                t.Status,
                t.Amount,
                t.Description,
                CreatedAt = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                PaidAt = t.PaidAt.HasValue ? t.PaidAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                User = t.User != null ? new
                {
                    t.User.Id,
                    t.User.Email,
                    t.User.UserName,
                    t.User.AvatarImage
                } : null // Xử lý khi User là null
            }).ToListAsync();

            return Ok(new
            {
                TotalTransactions = transactions.Count,
                Transactions = transactions
            });
        }

        [HttpGet("earnings-monthly-growth")]
        public async Task<IActionResult> GetEarningsMonthlyGrowth()
        {
            var now = DateTime.UtcNow;

            // Xác định khoảng thời gian của tháng hiện tại
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddSeconds(-1);

            // Xác định khoảng thời gian của tháng trước
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            var previousMonthEnd = currentMonthStart.AddSeconds(-1);

            // Tính tổng Earnings cho tháng hiện tại
            var currentMonthEarnings = await _context.Transactions
                .Where(t => t.Status == "PAID" && t.PaidAt >= currentMonthStart && t.PaidAt <= currentMonthEnd)
                .SumAsync(t => t.Amount);

            // Tính tổng Earnings cho tháng trước
            var previousMonthEarnings = await _context.Transactions
                .Where(t => t.Status == "PAID" && t.PaidAt >= previousMonthStart && t.PaidAt <= previousMonthEnd)
                .SumAsync(t => t.Amount);

            // Tính tỷ lệ tăng trưởng (%)
            double growthPercentage = 0;
            if (previousMonthEarnings > 0)
            {
                growthPercentage = (double)(((currentMonthEarnings - previousMonthEarnings) / previousMonthEarnings) * 100);
            }
            else if (currentMonthEarnings > 0)
            {
                growthPercentage = 100;
            }

            return Ok(new
            {
                CurrentMonthEarnings = currentMonthEarnings,
                PreviousMonthEarnings = previousMonthEarnings,
                GrowthPercentage = Math.Round(growthPercentage, 2)
            });
        }


    }
}
