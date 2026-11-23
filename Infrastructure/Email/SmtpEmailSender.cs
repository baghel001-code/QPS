using Application.Interfaces.V1.Notifications;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.AppSettings;
using Domain.Entities.Notifications;
using EmailService;
using EmailService.Models;
using MiniMapper;
using System.Net.Mail;


namespace Infrastructure.Email
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly MailSender _mailSender;
        private readonly IMapper _mapper;
        private readonly EmailServerSettings _serverSettings;
        public SmtpEmailSender(IMapper mapper, EmailServerSettings serverSettings, MailSender mailSender)
        {
            _mailSender = mailSender;
            _mapper = mapper;
            _serverSettings = serverSettings;
        }

        public Task<MailResponse> SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            var serverSettings = _mapper.Map<ServerSettings>(_serverSettings);

            List<MailAttachment> attachments = new List<MailAttachment>();
            foreach (var item in message.Attachments ?? Enumerable.Empty<MailAttachmentModel>())
            {
                attachments.Add(new MailAttachment
                {
                    FileName = item.FileName,
                    FilePath = item.FilePath,
                    ContentType = item.ContentType,
                    FileBytes = item.FileBytes
                });
            }

            var userSettings = new UserMailSettings
            {
                From = message.From,
                To = string.Join(";", message.To),
                Cc = string.Join(";", message.Cc),
                Bcc = string.Join(";", message.Bcc),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsBodyHtml,
                Attachments = attachments
            };

            var abc = _mailSender.SendMail(serverSettings, userSettings);
            return Task.Run(() => abc, ct);
            //return Task.Run(() => _mailSender.SendMail(serverSettings, userSettings), ct);

        }


    }
}
