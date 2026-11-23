using Application.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.Notifications
{
    public interface IRecipientResolver<in TModel>
    {
        Task<EmailRecipients> ResolveAsync(TModel model, CancellationToken ct = default);
        
    }
  
}
