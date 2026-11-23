using Application.Interfaces.V1.Notifications;
using Application.Notifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Notifications
{
    public sealed class RecipientPolicy : IRecipientPolicy
    {
        private static readonly string[] ReservedDomains = { "example.com", "example.net", "example.org", "localhost" };
        private static readonly string[] ReservedTlds = { ".test", ".example", ".invalid", ".localhost" };

        private readonly ILogger<RecipientPolicy> _logger;

        public RecipientPolicy(ILogger<RecipientPolicy> logger) => _logger = logger;

        public EmailRecipients Filter(EmailRecipients recipients, string notificationName)
        {
            if (recipients is null) return EmailRecipients.None;

            // Shared across fields: an address kept in To is not repeated in Cc/Bcc.
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return new EmailRecipients
            {
                To = Clean(recipients.To, "To", notificationName, seen),
                Cc = Clean(recipients.Cc, "Cc", notificationName, seen),
                Bcc = Clean(recipients.Bcc, "Bcc", notificationName, seen)
            };
        }

        private IReadOnlyList<string> Clean(IReadOnlyList<string> source, string field, string notificationName, HashSet<string> seen)
        {
            var result = new List<string>();
            foreach (var raw in source ?? Array.Empty<string>())
            {
                foreach (var email in Split(raw))
                {
                    if (!IsDeliverable(email, out var reason))
                    {
                        _logger.LogWarning("[{Notification}] Skipped {Field} recipient '{Email}': {Reason}",
                            notificationName, field, email, reason);
                        continue;
                    }
                    if (seen.Add(email))
                        result.Add(email);
                }
            }
            return result;
        }

        private static IEnumerable<string> Split(string? raw)
            => string.IsNullOrWhiteSpace(raw)
                ? Enumerable.Empty<string>()
                : raw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static bool IsDeliverable(string email, out string reason)
        {
            if (!MailAddress.TryCreate(email, out var addr) || addr.Address != email)
            {
                reason = "invalid format";
                return false;
            }

            var host = addr.Host.ToLowerInvariant();
            if (!host.Contains('.'))
            {
                reason = "domain has no TLD";
                return false;
            }
            if (ReservedDomains.Any(d => host == d || host.EndsWith("." + d)) ||
                ReservedTlds.Any(t => host.EndsWith(t)))
            {
                reason = "reserved domain (RFC 2606)";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
