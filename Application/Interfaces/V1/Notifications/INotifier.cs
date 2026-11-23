using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.Notifications
{
    public interface INotifier<in TModel>
    {
        Task<bool> NotifyAsync(TModel model, CancellationToken ct = default);
    }
}
