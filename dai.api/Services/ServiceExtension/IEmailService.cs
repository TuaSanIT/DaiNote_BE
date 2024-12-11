using dai.api.Helper;

namespace dai.api.Services.ServicesAPI
{
    public interface IEmailService
    {
        Task SendEmailAsync(Mailrequest mailrequest);
        Task SendInvitationEmailAsync(string toEmail, Guid boardId, string invitationCode, Guid senderUserId);
    }
}
