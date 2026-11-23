using Application.Interfaces.V1.User;
using Domain.Entities.Common;
using Domain.Entities.Request;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IUserValidationService _userValidationService;
        private readonly NavigationManager _navigationManager;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private bool _initialized;

        public int UserId { get; private set; }
        public string UserCode { get; private set; } = string.Empty;
        public string UserName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string MobileNo { get; private set; } = string.Empty;
        public string UserType { get; private set; } = string.Empty;
        public List<string> QAMAdminRegions { get; private set; } = new();
        public bool IsVendor { get; private set; }
        public bool IsBuyer { get; private set; }
        public bool IsQC { get; private set; }
        public bool IsView { get; private set; }
        public bool IsAdmin { get; private set; }
        public bool IsQAM { get; private set; }
        public bool IsQA { get; private set; }
        public bool IsQAAdmin { get; private set; }
        public bool IsACH { get; private set; }
        public bool IsCH { get; private set; }
        public bool IsMerchandiser { get; private set; }
        public bool IsBFT { get; private set; }
        public bool IsASN { get; private set; }
        public bool IsAdminstrator { get; private set; }
        public bool IsCM { get; private set; }
        public string RoleTypeCode => true switch
        {
            _ when IsVendor => "Vendor",
            _ when IsBuyer => "Buyer",
            _ when IsQC => "QC",
            _ when IsAdmin => "Admin",
            _ when IsQAM => "QAM",
            _ when IsQA => "QA",
            _ when IsQAAdmin => "QAAdmin",
            _ when IsACH => "ACH",
            _ when IsCH => "CH",
            _ when IsMerchandiser => "Merchandiser",
            _ when IsBFT => "BFT",
            _ when IsASN => "ASN",
            _ => "Unknown"
        };


        public CurrentUserService(AuthenticationStateProvider authStateProvider, IUserValidationService userValidationService, NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor)
        {
            _authStateProvider = authStateProvider;
            _userValidationService = userValidationService;
            _navigationManager = navigationManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CurrentUserResult> EnsureInitialized()
        {
            if (_initialized)
                return new CurrentUserResult { Status = CurrentUserStatus.Ok };

            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return new CurrentUserResult { Status = CurrentUserStatus.NoSession };

            var uid = user.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.User_Id)?.Value;
            if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var parsedId))
                return new CurrentUserResult { Status = CurrentUserStatus.InvalidUserId };

            UserId = parsedId;

            var rgns = user.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.RGNS)?.Value;
            QAMAdminRegions = !string.IsNullOrEmpty(rgns)
                ? rgns.Split(',').ToList()
                : new List<string>();

            UserCode = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            Email = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
            UserName = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            MobileNo = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.MobilePhone)?.Value ?? string.Empty;
            UserType = user.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.UserType)?.Value ?? "";


            IsVendor = user.IsInRole("Vendor");
            IsAdminstrator = user.IsInRole("1");
            IsBuyer = user.IsInRole("2");
            IsQC = user.IsInRole("3");
            IsView = user.IsInRole("4");
            IsAdmin = user.IsInRole("5");
            IsQA = user.IsInRole("1006");
            IsQAM = user.IsInRole("1007");
            IsQAAdmin = user.IsInRole("1008");
            IsCM = user.IsInRole("1009");
            IsCH = user.IsInRole("1010");
            IsACH = user.IsInRole("1011");
            IsMerchandiser = user.IsInRole("1012");
            IsBFT = user.IsInRole("1013");
            IsASN = user.IsInRole("1014");

            _initialized = true;
            return new CurrentUserResult { Status = CurrentUserStatus.Ok };
        }

        public async Task Logout()
        {
            try
            {
                if (!string.IsNullOrEmpty(UserType))
                {
                    IssueSessionRequest issueSessionRequest = new IssueSessionRequest(UserType, UserCode, UserId, null, null);
                    await _userValidationService.ClearSessionAsync(issueSessionRequest);
                }

            }
            catch (Exception ex)
            {
                // Don't block logout on this call failing — log and continue,
                // otherwise a network blip could trap the user on the page.
                // _logger.LogWarning(ex, "Failed to clear session token for user {UserId}", UserId);
            }
            finally
            {
                _initialized = false;
                _navigationManager.NavigateTo("/auth/logout", forceLoad: true);
            }
        }
        public string? ConsumeLoginNotice()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session == null)
                return null;

            var notice = httpContext.Session.GetString("LoginNotice");
            if (!string.IsNullOrEmpty(notice))
                httpContext.Session.Remove("LoginNotice"); // read once, then gone — a refresh won't retrigger it

            return notice;
        }
    }


}
