using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.Notifications
{
    public interface IEmailTemplate<in TModel>
    {
        string BuildSubject(TModel model);
        string BuildBody(TModel model);
    }
}
