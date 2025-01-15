using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using dai.dataAccess.IRepositories;
using dai.api.Services.ServicesAPI;

namespace dai.api.Helper
{
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ReminderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("ReminderService is running...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessReminders(stoppingToken);

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("ReminderService task was canceled.");
                }
            }
        }

        private async Task ProcessReminders(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var noteRepository = scope.ServiceProvider.GetRequiredService<INoteRepository>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var nowUtc = DateTime.UtcNow;

                try
                {
                    var dueNotes = await noteRepository.GetNotesDueForReminderAsync(nowUtc);
                    Console.WriteLine($"Number of notes due for reminder: {dueNotes.Count()}");

                    foreach (var note in dueNotes)
                    {
                        var user = await userRepository.GetUserByIdAsync(note.UserId);

                        if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.TimeZoneId))
                        {
                            Console.WriteLine($"Skipping note '{note.Title}' for user '{note.UserId}' due to missing email or TimeZoneId.");
                            continue;
                        }

                        Console.WriteLine($"Processing reminder for user: {user.Email}, TimeZoneId: {user.TimeZoneId}");

                        var userTimeZone = GetTimeZoneInfo(user.TimeZoneId);
                        var userReminderTime = TimeZoneInfo.ConvertTimeFromUtc(note.Reminder.Value, userTimeZone);
                        var userCurrentTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, userTimeZone);

                        Console.WriteLine($"User Reminder Time: {userReminderTime}, User Current Time: {userCurrentTime}");

                        if (userReminderTime <= userCurrentTime)
                        {
                            try
                            {
                                Console.WriteLine($"Sending reminder email to {user.Email} for note '{note.Title}'...");
                                await emailService.SendNoteRemiderNotificationAsync(user.Email, note.Title, userReminderTime);
                                Console.WriteLine($"Email sent successfully to {user.Email}.");

                                // Mark the reminder as sent
                                note.ReminderSent = true;
                                await noteRepository.UpdateNoteAsync(note);
                                Console.WriteLine($"Marked note '{note.Title}' as reminder sent.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to send email to {user.Email}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing note reminders: {ex.Message}");
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
                Console.WriteLine($"Invalid TimeZoneId: {timeZoneId}. Falling back to UTC.");
                return TimeZoneInfo.Utc; // Fallback to UTC if invalid
            }
        }
    }

}
