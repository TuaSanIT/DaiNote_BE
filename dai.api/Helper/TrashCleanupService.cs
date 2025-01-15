using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using dai.dataAccess.IRepositories;
using dai.api.Services.ServicesAPI;
using dai.api.Services.ServiceExtension;

namespace dai.api.Helper
{
    public class TrashCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public TrashCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessTrashCleanup(stoppingToken);

                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
                }
                catch (TaskCanceledException)
                {
                    // Handle cancellation
                }
            }
        }

        private async Task ProcessTrashCleanup(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var noteRepository = scope.ServiceProvider.GetRequiredService<INoteRepository>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var azureBlobService = scope.ServiceProvider.GetRequiredService<AzureBlobService>();

                var nowUtc = DateTime.UtcNow;

                // Notify users about notes close to being deleted
                try
                {
                    var trashedNotes = await noteRepository.GetNotesOlderThanTrashDateAsync(nowUtc.AddDays(-4));
                    foreach (var note in trashedNotes)
                    {
                        var user = await userRepository.GetUserByIdAsync(note.UserId);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            var userTimeZone = GetTimeZoneInfo(user.TimeZoneId);
                            var userThresholdDate = TimeZoneInfo.ConvertTimeFromUtc(note.TrashDate.AddDays(7), userTimeZone);

                            await emailService.SendNoteDeletionNotificationAsync(
                                user.Email,
                                note.Title,
                                userThresholdDate
                            );

                            note.TrashIsNotified = true;
                            await noteRepository.UpdateNoteAsync(note);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error notifying users about trashed notes: {ex.Message}");
                }

                // Permanently delete notes older than 7 days in trash
                try
                {
                    var notesToDelete = await noteRepository.GetNotesToDeleteAsync(nowUtc.AddDays(-6));
                    foreach (var note in notesToDelete)
                    {
                        if (note.Images != null && note.Images.Any())
                        {
                            foreach (var imageUrl in note.Images)
                            {
                                await azureBlobService.DeleteNoteImageAsync(imageUrl);
                            }
                        }

                        await noteRepository.DeleteNoteAsync(note.Id);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting trashed notes: {ex.Message}");
                }
            }
        }

        private TimeZoneInfo GetTimeZoneInfo(string? timeZoneId)
        {
            try
            {
                return !string.IsNullOrEmpty(timeZoneId)
                    ? TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)
                    : TimeZoneInfo.Utc;
            }
            catch
            {
                return TimeZoneInfo.Utc; // Fallback to UTC if invalid
            }
        }
    }

}