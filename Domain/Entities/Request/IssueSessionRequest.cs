using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Request
{
    public class IssueSessionRequest
    {
        public string UserType { get; }
        public string UserCode { get; }
        public int UserId { get; }
        public string? DeviceInfo { get; }
        public string SessionToken { get { return _SessionToken; } }
        private string _SessionToken;
        public IssueSessionRequest(string userType, string userCode, int userId, string? sessionToken = null, string? deviceInfo = null)
        {
            if (string.IsNullOrWhiteSpace(userType))
                throw new ArgumentException("UserType is required.", nameof(userType));

            if (string.IsNullOrWhiteSpace(userCode))
                throw new ArgumentException("UserCode is required.", nameof(userCode));

            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be a positive value.");

            UserType = userType;
            UserCode = userCode;
            UserId = userId;
            DeviceInfo = deviceInfo;
            _SessionToken = sessionToken ?? GenerateSessionToken();
        }

        private static string GenerateSessionToken() => Guid.NewGuid().ToString("N"); // 32-char random token

        public void SetSessionToken(string Token) => _SessionToken = Token;
    }
}
