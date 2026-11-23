using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.User
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string UserCode { get; }
        string UserName { get;}
        string Email { get;  }
        string MobileNo { get;  } 
        List<string> QAMAdminRegions { get; }
        bool IsVendor { get; }
        bool IsBuyer { get; }
        bool IsQC { get; }
        bool IsView { get; }
        bool IsAdmin { get; }
        bool IsQAM { get; }
        bool IsQA { get; }
        bool IsQAAdmin { get; }
        bool IsACH { get; }
        bool IsCH { get; }
        bool IsMerchandiser { get; }
        bool IsBFT { get; }
        bool IsASN { get; }
        bool IsAdminstrator { get; }
        bool IsCM { get; }
        string RoleTypeCode { get; }
        Task<CurrentUserResult> EnsureInitialized();

        Task Logout();
        string? ConsumeLoginNotice();
    }
}
