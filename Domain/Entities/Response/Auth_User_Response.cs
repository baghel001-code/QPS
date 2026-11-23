using Domain.Entities.Base;
using Domain.Entities.Modals;
using Domain.Enums;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Response
{
    public sealed class Auth_User_Response : BaseResponse
    {
        public Auth_User_Row? USER { get; set; }
        public Modules Modules { get; set; } = new();
        public List<Menu> MenuList { get; set; } = new();

        public AuthUser? AuthUser => USER is null ? null : ToAuthUser(USER, Modules);
        private static AuthUser? ToAuthUser(Auth_User_Row u, Modules? access)
        {
            if (!Enum.TryParse<AccountType>(u.ACCOUNTTYPE, ignoreCase: true, out var accountType))
                return null;   // bad/missing account type: treat as no usable user

            var userName = string.IsNullOrWhiteSpace(u.USERNAME) ? u.USERCODE : u.USERNAME;

            var modules = new List<string>();
            if (access is not null)
            {
                if (access.Menu) modules.Add("MENU");
                if (access.CMS) modules.Add("CMS");
                if (access.Media) modules.Add("MEDIA");
                if (access.User_Management) modules.Add("USER_MANAGEMENT");
            }

            return new AuthUser
            (
                Id: u.ID,
                UserCode: u.USERCODE,
                UserName: u.USERNAME,
                MobileNumber: u.MOBILENUMBER ?? "",
                Email: u.EMAIL ?? "",
                DisplayName: string.IsNullOrWhiteSpace(u.DISPLAYNAME) ? userName : u.DISPLAYNAME,
                PasswordHash: u.PASSWORDHASH ?? "",
                SecurityStamp: u.SECURITYSTAMP ?? "",
                IsActive: u.ISACTIVE,
                FailedLoginCount: u.FAILEDLOGINCOUNT ?? 0,
                LockoutEndUtc: u.LOCKOUTENDUTC ?? u.LOCKOUTENDUTC,
                Roles: u.ROLEID is int roleId ? new[] { roleId.ToString() } : Array.Empty<string>(),
                RegionIds: u.REGION_IDS ?? "",
                SessionToken: u.SECURITYSTAMP ?? "",
                GSTNumber: u.GST_NO ?? "",
                UserType: u.ACCOUNTTYPE ?? "",
                UserModules: access ?? new Modules(),
                AccountType: accountType
            )
            {
                MustChangePassword = u.MUSTCHANGEPASSWORD ?? false,
                Modules = modules,
            };
        }
    }

    public sealed record ResetTokenOwner(string USER_CODE, string ACCOUNT_TYPE);
}
