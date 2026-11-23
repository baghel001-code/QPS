using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class PpsStatusChangedTemplate : IEmailTemplate<PpsStatusChangedModel>
    {
        public string BuildSubject(PpsStatusChangedModel m) => "PPS request creation...reg.";

        public string BuildBody(PpsStatusChangedModel m) =>
            $"Hi Team!, <br/>PPS request has been successfully <b>{m.Action}</b> " +
            $"for generic article <h3>{m.ArticleDescription} [ {m.GenericArticle} ]. </h3>"+
            EmailFooter.Footer(EmailFooter.PPSNote);
    }
}
