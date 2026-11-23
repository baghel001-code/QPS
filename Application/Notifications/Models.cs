using Application.DTOs.Asn;
using Domain.Entities.Modals;
using Domain.Entities.Notifications;
using Domain.Entities.Response;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications
{

    public sealed record PpsStatusChangedModel(string Action, string ArticleDescription, string GenericArticle, string BuyerId);
    public sealed record PpsRequestCreatedModel(string ArticleDescription, string GenericArticle);

    public sealed record InspectionCreatedModel(POHeader PO, int RecordId);
    public sealed record VendorAccountModel(Vendor Vendor, string PlainPassword, bool IsUpdate, string? DefaultEmailId = null);

    public sealed record ForgotPasswordModel(Vendor Vendor, string PlainPassword);

    public sealed record PasswordResetLinkModel(AuthUser Vendor, string ResetLink, TimeSpan Expiryin);
   
    public sealed record DoiConfirmedModel(InspectionAssignRowDto Row);
    public sealed record DailyScheduleModel(EmailRecipient Schedule);

    public sealed record AsnGeneratedModel(AsnGeneratedNotificationDto AsnDetails);

    public sealed record TestMailModel(string UserCode, string UserName, string Email, DateTimeOffset LoginTimeUtc, string IpAddress, string UserAgent, string remark);

    public sealed record EmailRecipients
    {
        public IReadOnlyList<string> To { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Cc { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Bcc { get; init; } = Array.Empty<string>();

        public static EmailRecipients None { get; } = new();

        public bool IsEmpty => To.Count == 0 && Cc.Count == 0 && Bcc.Count == 0;

        /// <summary>Shortcut for the common "To only" case. Nulls and blanks are allowed; the policy removes them.</summary>
        public static EmailRecipients ToOnly(params string?[] to)
            => new() { To = to.Where(e => e is not null).Select(e => e!).ToArray() };
    }

}
