using Domain.Entities.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.User
{
    public interface IUserValidationService
    {
        Task<bool> IsUserActiveAsync(string empCode, CancellationToken cancellationToken);
        Task<bool> IsSessionValidAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default);
        Task<string> IssueNewSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default);
        Task ClearSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default);
        Task<bool> HasActiveSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default);
    }
}
