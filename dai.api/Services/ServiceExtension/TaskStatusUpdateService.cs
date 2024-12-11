using dai.dataAccess.DbContext;

namespace dai.api.Services.ServiceExtension;

public class TaskStatusUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public TaskStatusUpdateService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateTaskStatuses();
            await Task.Delay(TimeSpan.FromHours(12), stoppingToken); 
        }
    }

    private async Task UpdateTaskStatuses()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateTime.Now;

            var tasksToUpdate = dbContext.Tasks
                .Where(t => t.Finish_At < today && t.Status == "ongoing")
                .ToList();

            foreach (var task in tasksToUpdate)
            {
                task.Status = "over";
                task.Update_At = DateTime.Now; 
            }

            if (tasksToUpdate.Any())
            {
                await dbContext.SaveChangesAsync();
                Console.Write($"{tasksToUpdate.Count} - {Task.CurrentId} task(s) updated to 'over'.");
            }
        }
    }
}
