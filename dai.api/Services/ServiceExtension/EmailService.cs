using dai.api.Helper;
using dai.dataAccess.IRepositories;
using dai.dataAccess.Repositories;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace dai.api.Services.ServicesAPI
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings emailSettings;
        private readonly ICollaboratorRepository _collaboratorRepository;
        private readonly IUserRepository _userRepository;

        public EmailService(IOptions<EmailSettings> options, ICollaboratorRepository collaboratorRepository, IUserRepository userRepository)
        {
            this.emailSettings = options.Value;
            _collaboratorRepository = collaboratorRepository;
            _userRepository = userRepository;
        }
        public async Task SendEmailAsync(Mailrequest mailrequest)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(emailSettings.Email);
            email.To.Add(MailboxAddress.Parse(mailrequest.ToEmail));
            email.Subject = mailrequest.Subject;
            var builder = new BodyBuilder();


            byte[] fileBytes;
            if (System.IO.File.Exists("Attachment/dummy.pdf"))
            {
                FileStream file = new FileStream("Attachment/dummy.pdf", FileMode.Open, FileAccess.Read);
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    fileBytes = ms.ToArray();
                }
                builder.Attachments.Add("attachment.pdf", fileBytes, ContentType.Parse("application/octet-stream"));
                builder.Attachments.Add("attachment2.pdf", fileBytes, ContentType.Parse("application/octet-stream"));
            }

            builder.HtmlBody = mailrequest.Body;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(emailSettings.Host, emailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(emailSettings.Email, emailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        public async Task SendInvitationEmailAsync(string toEmail, Guid boardId, string invitationCode, Guid senderUserId)
        {

            var board = await _collaboratorRepository.GetBoardByIdAsync(boardId);
            if (board == null)
            {
                throw new Exception("Board not found");
            }

            var boardName = board.Name;


            var senderUser = await _userRepository.GetUserByIdAsync(senderUserId);
            if (senderUser == null)
            {
                throw new Exception("Sender user not found");
            }

            var senderName = senderUser.UserName;


            var email = new MimeMessage();
            email.Sender = new MailboxAddress(senderName, emailSettings.Email);
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"You've been invited to collaborate on the board: {boardName}";

            var builder = new BodyBuilder
            {
                HtmlBody = $@"
    <html>
        <head>
            <style>
                body {{
                    font-family: 'Arial', sans-serif;
                    margin: 0;
                    padding: 0;
                    background-color: #f6f8fa;
                    color: #24292f;
                }}
                .container {{
                    width: 100%;
                    max-width: 600px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    padding: 20px;
                    border-radius: 6px;
                    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12), 0 1px 2px rgba(0, 0, 0, 0.24);
                }}
                .header {{
                    text-align: center;
                    margin-bottom: 20px;
                }}
                .header h1 {{
                    font-size: 24px;
                    color: #0366d6;
                    margin: 0;
                }}
                .content {{
                    margin-bottom: 30px;
                    font-size: 16px;
                    line-height: 1.5;
                }}
                .cta-button {{
                    display: inline-block;
                    background-color: #28a745;
                    color: #ffffff;
                    padding: 12px 24px;
                    font-size: 16px;
                    text-align: center;
                    text-decoration: none;
                    border-radius: 6px;
                    margin-top: 20px;
                    transition: background-color 0.2s ease;
                }}
                .cta-button:hover {{
                    background-color: #218838;
                }}
                .footer {{
                    font-size: 14px;
                    text-align: center;
                    color: #6a737d;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>You're Invited to Collaborate on {boardName}</h1>
                </div>
                <div class='content'>
                    <p>Hello!</p>
                    <p>You have been invited to collaborate on the board <strong>{boardName}</strong> by username <strong>{senderName}</strong>.</p>
                    <p>To accept the invitation, simply click the button below:</p>
                    <a href='https://dainote.netlify.app/accept-invitation?code={invitationCode}' class='cta-button'>Accept Invitation</a>
                </div>
                <div class='footer'>
                    <p>If you did not expect this invitation, you can safely ignore this email.</p>
                    <p>Best regards,<br>Dai Note</p>
                </div>
            </div>
        </body>
    </html>"
            };


            email.Body = builder.ToMessageBody();


            using var smtp = new SmtpClient();
            smtp.Connect(emailSettings.Host, emailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(emailSettings.Email, emailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }
        public async Task SendNoteDeletionNotificationAsync(string toEmail, string noteTitle, DateTime deletionDate)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(emailSettings.Email);
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Reminder: Your Note Will Be Deleted Soon";

            var builder = new BodyBuilder
            {
                HtmlBody = $@"
        <html>
           <head>
            <style>
                body {{
                    font-family: 'Arial', sans-serif;
                    margin: 0;
                    padding: 0;
                    background-color: #f6f8fa;
                    color: #24292f;
                }}
                .container {{
                    width: 100%;
                    max-width: 600px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    padding: 20px;
                    border-radius: 6px;
                    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12), 0 1px 2px rgba(0, 0, 0, 0.24);
                }}
                .header {{
                    text-align: center;
                    margin-bottom: 20px;
                }}
                .header h1 {{
                    font-size: 24px;
                    color: #0366d6;
                    margin: 0;
                }}
                .content {{
                    margin-bottom: 30px;
                    font-size: 16px;
                    line-height: 1.5;
                }}
                .cta-button {{
                    display: inline-block;
                    background-color: #28a745;
                    color: #ffffff;
                    padding: 12px 24px;
                    font-size: 16px;
                    text-align: center;
                    text-decoration: none;
                    border-radius: 6px;
                    margin-top: 20px;
                    transition: background-color 0.2s ease;
                }}
                .cta-button:hover {{
                    background-color: #218838;
                }}
                .footer {{
                    font-size: 14px;
                    text-align: center;
                    color: #6a737d;
                }}
            </style>
        </head>
            <body>
                <div class='container'>
                    <h1>Important: Note Deletion Notification</h1>
                    <p>Hello,</p>
                    <p>This is a reminder that your note titled <strong>{noteTitle}</strong> is scheduled for deletion on <strong>{deletionDate:MMMM dd, yyyy}</strong>.</p>
                    <p>If you want to retain this note, please take action before the scheduled deletion date.</p>
                    <p>Thank you for using our service!</p>
                    <div class='footer'>
                        <p>Dai Note Team</p>
                    </div>
                </div>
            </body>
        </html>"
            };

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(emailSettings.Host, emailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(emailSettings.Email, emailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        public async Task SendNoteRemiderNotificationAsync(string toEmail, string noteTitle, DateTime reminderDate)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(emailSettings.Email);
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "You have a reminder on your note";

            var builder = new BodyBuilder
            {
                HtmlBody = $@"
        <html>
           <head>
            <style>
                body {{
                    font-family: 'Arial', sans-serif;
                    margin: 0;
                    padding: 0;
                    background-color: #f6f8fa;
                    color: #24292f;
                }}
                .container {{
                    width: 100%;
                    max-width: 600px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    padding: 20px;
                    border-radius: 6px;
                    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12), 0 1px 2px rgba(0, 0, 0, 0.24);
                }}
                .header {{
                    text-align: center;
                    margin-bottom: 20px;
                }}
                .header h1 {{
                    font-size: 24px;
                    color: #0366d6;
                    margin: 0;
                }}
                .content {{
                    margin-bottom: 30px;
                    font-size: 16px;
                    line-height: 1.5;
                }}
                .cta-button {{
                    display: inline-block;
                    background-color: #28a745;
                    color: #ffffff;
                    padding: 12px 24px;
                    font-size: 16px;
                    text-align: center;
                    text-decoration: none;
                    border-radius: 6px;
                    margin-top: 20px;
                    transition: background-color 0.2s ease;
                }}
                .cta-button:hover {{
                    background-color: #218838;
                }}
                .footer {{
                    font-size: 14px;
                    text-align: center;
                    color: #6a737d;
                }}
            </style>
        </head>
            <body>
                <div class='container'>
                    <h1>Important: Note Deletion Notification</h1>
                    <p>Hello,</p>
                    <p>This is a reminder that your note titled <strong>{noteTitle}</strong> is scheduled on <strong>{reminderDate:MMMM dd, yyyy}</strong>.</p>
                    <p>If you want to retain this note, please take action before the scheduled deletion date.</p>
                    <p>Thank you for using our service!</p>
                    <div class='footer'>
                        <p>Dai Note Team</p>
                    </div>
                </div>
            </body>
        </html>"
            };

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(emailSettings.Host, emailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(emailSettings.Email, emailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        public async Task SendTaskDeadlineReminderAsync(string toEmail, string taskTitle, DateTime deadline)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(emailSettings.Email);
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Task Deadline Reminder";

            var builder = new BodyBuilder
            {
                HtmlBody = $@"
<head>
            <style>
                body {{
                    font-family: 'Arial', sans-serif;
                    margin: 0;
                    padding: 0;
                    background-color: #f6f8fa;
                    color: #24292f;
                }}
                .container {{
                    width: 100%;
                    max-width: 600px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    padding: 20px;
                    border-radius: 6px;
                    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12), 0 1px 2px rgba(0, 0, 0, 0.24);
                }}
                .header {{
                    text-align: center;
                    margin-bottom: 20px;
                }}
                .header h1 {{
                    font-size: 24px;
                    color: #0366d6;
                    margin: 0;
                }}
                .content {{
                    margin-bottom: 30px;
                    font-size: 16px;
                    line-height: 1.5;
                }}
                .cta-button {{
                    display: inline-block;
                    background-color: #28a745;
                    color: #ffffff;
                    padding: 12px 24px;
                    font-size: 16px;
                    text-align: center;
                    text-decoration: none;
                    border-radius: 6px;
                    margin-top: 20px;
                    transition: background-color 0.2s ease;
                }}
                .cta-button:hover {{
                    background-color: #218838;
                }}
                .footer {{
                    font-size: 14px;
                    text-align: center;
                    color: #6a737d;
                }}
            </style>
        </head>
        <html>
            <body>
                <h1>Task Deadline Reminder</h1>
                <p>This is a reminder that the task titled <strong>{taskTitle}</strong> is due on <strong>{deadline:MMMM dd, yyyy}</strong>.</p>
                <p>Please ensure to complete the task on time.</p>
            </body>
        </html>"
            };

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(emailSettings.Host, emailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(emailSettings.Email, emailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

    }
}