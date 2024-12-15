using dai.dataAccess.DbContext;
using Microsoft.EntityFrameworkCore;

namespace dai.api.Services.ServiceExtension
{
    public class VipExpiryCheckerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public VipExpiryCheckerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var now = DateTime.UtcNow;


                    var expiredUsers = await context.Users
                            .Where(u => u.IsVipSupplier == true && u.VipExpiryDate <= now)
                            .ToListAsync();


                    foreach (var user in expiredUsers)
                    {
                        user.IsVipSupplier = false;
                        user.VipExpiryDate = null; // Xóa ngày hết hạn
                    }

                    await context.SaveChangesAsync();
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Kiểm tra hàng ngày
            }
        }
    }
}
