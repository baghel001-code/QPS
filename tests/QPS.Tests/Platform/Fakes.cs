using Domain.Entities.Modals;
using Domain.Enums;
using Domain.Records;

namespace QPS.Tests.Platform
{
    /// <summary>A clock the tests move by hand.</summary>
    public sealed class FakeClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(TimeSpan by) => Now = Now.Add(by);
    }

    /// <summary>Builds AuthUser records with sensible defaults.</summary>
    public static class Users
    {
        public static AuthUser Employee(string userCode = "E1001", string userName = "rohit.patel", params string[] roles) =>
            new(1, userCode, userName, "9999999999", $"{userName}@example.com", "Rohit Patel", "", "stamp-1",
                IsActive: true, FailedLoginCount: 0, LockoutEndUtc: null, Roles: roles, RegionIds: "1", SessionToken: "session-1",
                GSTNumber: "", UserType: "EMPLOYEE", UserModules: new Modules(), AccountType: AccountType.Employee);

        public static AuthUser Vendor(string userCode = "V2001", string userName = "acme") =>
            new(2, userCode, userName, "8888888888", $"{userName}@vendor.example.com", "Acme Textiles", "", "stamp-2",
                IsActive: true, FailedLoginCount: 0, LockoutEndUtc: null, Roles: [Admin.Auth.QpsRoles.Vendor], RegionIds: "",
                SessionToken: "session-2", GSTNumber: "27AAAAA0000A1Z5", UserType: "VENDOR", UserModules: new Modules(),
                AccountType: AccountType.Vendor);
    }
}
