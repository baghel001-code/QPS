using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class PpsRequestCreatedTemplate : IEmailTemplate<PpsRequestCreatedModel>
    {
        public string BuildSubject(PpsRequestCreatedModel m) => "PPS request creation...reg.";

        public string BuildBody(PpsRequestCreatedModel m) =>
            $"Hi Team!, <br/>PPS request has been successfully <b>created</b> " +
            $"for generic article <h3>{m.ArticleDescription} [ {m.GenericArticle} ]. </h3>"+
            EmailFooter.Footer(EmailFooter.PPSNote);
    }
}
