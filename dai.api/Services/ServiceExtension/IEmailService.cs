using dai.api.Helper;

namespace dai.api.Services.ServicesAPI
{
    public interface IEmailService
    {
        Task SendEmailAsync(Mailrequest mailrequest);
        Task SendInvitationEmailAsync(string toEmail, Guid boardId, string invitationCode, Guid senderUserId);
        Task SendNoteDeletionNotificationAsync(string toEmail, string noteTitle, DateTime deletionDate);
        Task SendNoteRemiderNotificationAsync(string toEmail, string noteTitle, DateTime reminderDate);
        Task SendTaskDeadlineReminderAsync(string toEmail, string taskTitle, DateTime deadline);
    }
}
