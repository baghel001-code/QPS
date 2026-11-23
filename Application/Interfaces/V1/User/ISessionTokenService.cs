using Domain.Entities.Request;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.User
{
    public interface ISessionTokenService
    {
        Task<IssueSessionResponce> IssueNewSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default);
        Task<IssueSessionResponce> IsSessionValidAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default);
        Task ClearSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default);
    }
}
