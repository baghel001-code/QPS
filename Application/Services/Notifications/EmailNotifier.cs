using Application.Interfaces.V1.Notifications;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Entities.AppSettings;
using Domain.Entities.Notifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Notifications
{
    public sealed class EmailNotifier<TModel> : INotifier<TModel>
    {
        private readonly IEmailTemplate<TModel> _template;
        private readonly IRecipientResolver<TModel> _recipients;
        private readonly IEmailSender _sender;
        private readonly EmailServerSettings _settings;
        private readonly ILogger<EmailNotifier<TModel>> _logger;
        private readonly IAttachmentProvider<TModel>? _attachments;
        private readonly IRecipientPolicy _recipientPolicy;

        public EmailNotifier(
            IEmailTemplate<TModel> template,
            IRecipientResolver<TModel> recipients,
            IEmailSender sender,
            EmailServerSettings settings,
            ILogger<EmailNotifier<TModel>> logger,
            IEnumerable<IAttachmentProvider<TModel>> attachmentProviders,
            IRecipientPolicy recipientPolicy)
        {
            _template = template;
            _recipients = recipients;
            _sender = sender;
            _settings = settings;
            _logger = logger;
            _attachments = attachmentProviders.FirstOrDefault();
            _recipientPolicy = recipientPolicy;
        }

        public async Task<bool> NotifyAsync(TModel model, CancellationToken ct = default)
        {
            var name = typeof(TModel).Name;
            try
            {
                var emailRecipients = await _recipients.ResolveAsync(model, ct);
                var recipients = _recipientPolicy.Filter(emailRecipients, name);

                if (recipients.To.Count == 0)   // SMTP needs at least one To; a Cc-only mail is usually a resolver bug
                {
                    _logger.LogWarning("[{Notification}] No deliverable To recipients; email not sent", name);
                    return false;
                }
               
                var message = new EmailMessage
                {
                    From = _settings.From,
                    To = recipients.To,
                    Cc = recipients.Cc,
                    Bcc = recipients.Bcc,
                    Subject = _template.BuildSubject(model),
                    Body = _template.BuildBody(model),
                    IsBodyHtml = true,
                    Attachments = _attachments?.GetAttachments(model)
                };

                var result = await _sender.SendAsync(message, ct);
                return result.isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending {Notification} notification", name);
                return false;
            }
        }
    }
}
