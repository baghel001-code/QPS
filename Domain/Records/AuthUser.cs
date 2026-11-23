using Domain.Entities.Modals;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Records
{
    /// <summary>What the auth module needs to know about a user. Map it from your own user table.</summary>
    /// <param name="Id">Unique across employees AND vendors, e.g. "E:1042" / "V:88", because sessions look users up by id only.</param>
    /// <param name="Roles">Role IDs from the role table ("1", "2", "1006"…, see QpsRoles); vendors: "Vendor".</param>
    /// <param name="SecurityStamp">Random value that changes whenever credentials change; a mismatch ends existing sessions.</param>
    public sealed record AuthUser(
    int Id,
    string UserCode,
    string UserName,
    string MobileNumber,
    string Email,
    string DisplayName,
    string PasswordHash,
    string SecurityStamp,
    bool IsActive,
    int FailedLoginCount,
    DateTimeOffset? LockoutEndUtc,
    IReadOnlyList<string> Roles,
    string RegionIds,
    string SessionToken,
    string GSTNumber,
    string UserType,
    Modules UserModules,
    AccountType AccountType)
    {
        /// <summary>
        /// True while the user still has a default/temporary password (the old page's
        /// Default_Password_Changed == false). They land on the change-password page first.
        /// </summary>
        public bool MustChangePassword { get; init; }

        /// <summary>
        /// Module flags from the old MenuService.GetModules(): "User_Management", "Menu".
        /// They add menu groups; see Navigation/AppMenu.cs and QpsRoles.Module*.
        /// </summary>
        public IReadOnlyList<string> Modules { get; init; } = [];
    }

}
