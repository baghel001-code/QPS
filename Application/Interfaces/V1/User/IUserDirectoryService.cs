using Domain.Entities.Modals;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.User
{
    public interface IUserDirectoryService
    {
        Task<List<UserDetails>> GetAllUsersAsync(CancellationToken ct = default);
        Task<UserLookupResult> FindUserByEmpCodeAsync(string empCode, CancellationToken ct = default);
        
    }
}
