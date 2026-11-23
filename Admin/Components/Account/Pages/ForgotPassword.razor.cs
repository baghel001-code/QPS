using Admin.Services;
using Application.Interfaces.V1;
using Application.Interfaces.V1.Notifications;
using Application.Notifications;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using EmailService.Helper;
using EmailService.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using MudBlazor;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using static System.Net.WebRequestMethods;

namespace Admin.Components.Account.Pages
{
    public partial class ForgotPassword
    {
        [Inject] protected INotifier<ForgotPasswordModel> ForgotPasswordNotifier { get; set; } = default!;

        private string _email = string.Empty;
        private string _maskedEmail = string.Empty;
        private bool _isProcessing;
        private string VendorCode { get; set; }
        public Vendor _vendor { get; set; } = new();
        private HttpContext httpContext { get; set; } = default!;
        private class emails
        {
            public string email { get; set; }
            public string masked_email { get; set; }
        }
        List<emails> maskemail = new();

       
        protected override async Task OnInitializedAsync()
        {
            var ctx = HttpContextAccessor.HttpContext;

            if (ctx != null)
            {
                httpContext = ctx;
            }
        }
       
        public void Dispose()
        {
            State.OnChange -= StateHasChanged;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var vcode = httpContext.Session.GetString("VendorCode");
                if (!string.IsNullOrEmpty(vcode))
                {
                    VendorCode = vcode;
                    var result = await _vendorLookup.GetByCodeAsync(VendorCode);
                    if (!result.Found)
                    {
                        Snackbar.Add($"Vendor [{VendorCode}] not found", Severity.Error);
                        return;
                    }
                    _vendor = result.Vendor!;
                    ShowMaskedEmail();
                   
                }

            }
            return;
        }



        private void ShowMaskedEmail()
        {
            if (_vendor != null)
            {

                var defaultEmailIds = _vendor.VendorAddresses.Where(a => a.IsDefault == true).FirstOrDefault();
                if (defaultEmailIds != null)
                {
                    foreach (var emailId in defaultEmailIds.Email_Ids.Split(';'))
                    {
                        if (string.IsNullOrWhiteSpace(emailId) || !emailId.Contains('@'))
                        {
                            _maskedEmail = string.Empty;
                            return;
                        }
                        else
                        {
                            maskemail.Add(new emails { email = emailId, masked_email = MaskEmailAdvanced(emailId) });
                        }

                    }

                }
                _maskedEmail = string.Join(", ", maskemail.Select(x => x.masked_email));
                StateHasChanged();
            }

        }

        private async Task ForgotPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(_email))
            {
                Snackbar.Add("Please enter your email.", Severity.Error);
                return;
            }

            _isProcessing = true;

            try
            {
                
                if (maskemail.Any(a => a.email == _email))
                {
                    var randomPassword = _passwordHasher.GenerateRandomPassword(8);
                    _vendor.PassWord = _passwordHasher.HashPassword(randomPassword);
                    _vendor.Default_Password_Changed = false;
                    var res1 = await ApiMasterService.CallingAPI<GenRes, Vendor>(AllApiNames.SET_VENDOR!, _vendor);
                    if (res1.responseCode.Equals(0))
                    {
                        await ForgotPasswordNotifier.NotifyAsync(new ForgotPasswordModel(_vendor, randomPassword));

                        _vendor = new();
                        maskemail = new();
                        _maskedEmail = "";
                        httpContext.Session.Remove("VendorCode");
                        StateHasChanged();
                        Snackbar.Add("Your new password has been sent to your email successfully!", Severity.Success);
                        await Task.Delay(1000);
                        NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
                    }
                    else
                    {
                        Snackbar.Add(res1.responseMessage!, Severity.Error);
                    }
                }
                else
                {
                    Snackbar.Add("Please enter registered email id.", Severity.Info);
                }

            }
            catch (Exception ex)
            {
                Snackbar.Add("Something went wrong. Please try again later.", Severity.Error);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public static string MaskEmailAdvanced(string email)
        {
            email = email.Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            var name = parts[0];
            var domain = parts[1];

            if (name.Length <= 2)
                return new string('*', name.Length) + "@" + domain;

            var first = name[0];
            var last = name[name.Length - 1];
            var stars = new string('*', name.Length - 2);

            return $"{first}{stars}{last}@{domain}";
        }
        public static string MaskEmailUsernameTwoHalves(string email)
        {
            email = email.Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            var username = parts[0];
            var domain = parts[1];

            if (username.Length <= 4)
                return new string('*', username.Length) + "@" + domain;

            // Split username into two halves
            int mid = username.Length / 2;
            string firstHalf = username.Substring(0, mid);
            string secondHalf = username.Substring(mid);

            // Mask middle of each half
            string maskedFirst = MaskHalf(firstHalf);
            string maskedSecond = MaskHalf(secondHalf);

            return $"{maskedFirst}{maskedSecond}@{domain}";
        }

        private static string MaskHalf(string input)
        {
            if (input.Length <= 2)
                return new string('*', input.Length);

            int prefix = 2;
            int suffix = 1;

            string start = input.Substring(0, prefix);
            string end = input.Substring(input.Length - suffix, suffix);

            string masked = new string('*', input.Length - (prefix + suffix));

            return $"{start}{masked}{end}";
        }


        private void BacktoLogin()
        {
            NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
        }
    }

}
