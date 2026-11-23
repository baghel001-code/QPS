using Domain.Entities.Modals;
using Domain.Entities.Response;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1
{
    public interface IInspectionDetailLoader
    {
        Task<POHeader?> LoadAsync(InspectionDetailRequest request);
        
    }
}
