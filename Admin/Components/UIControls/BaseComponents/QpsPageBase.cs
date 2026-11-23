using Application.Interfaces.V1.User;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Admin.Components.UIControls.BaseComponents
{
    public abstract class QpsPageBase : ComponentBase
    {
        [Inject] protected ICurrentUserService _CurrentUser { get; set; } = default!;
        [Inject] protected ISnackbar _Snackbar { get; set; } = default!;
        [Inject] protected NavigationManager _NavigationManager { get; set; } = default!;

        [Inject] protected IDialogService _dialogService { get; set; } = default!;
        [Inject] protected PODownloadService _PODownloadService { get; set; } = default!;
        [Inject] protected IJSRuntime _jS { get; set; } = default!;


        protected override async Task OnInitializedAsync()
        {
            try
            {
                var result = await _CurrentUser.EnsureInitialized();
                if (result.Status != CurrentUserStatus.Ok)
                {
                    var message = result.Status == CurrentUserStatus.NoSession
                        ? "Session expired. Please refresh the page."
                        : "User session is invalid. Please log in again.";
                    _Snackbar.Add(message, Severity.Error);
                    _NavigationManager.NavigateTo("/access-denied");
                    return;
                }

                await OnUserInitializedAsync();
            }
            catch (Exception ex)
            {
                _Snackbar.Add($"Error initializing page: {ex.Message}", Severity.Error);
            }
        }

        // Derived pages override this instead of OnInitializedAsync
        protected virtual Task OnUserInitializedAsync() => Task.CompletedTask;

        protected async Task ViewReport(int INSPECT_ID)
        {
            if (INSPECT_ID > 0)
            {
                var parameters = new DialogParameters
                {
                    ["InspectId"] = INSPECT_ID,
                    ["IsPreview"] = false
                };
                await _dialogService.ShowAsync<InspectionReportView>("Final Inspection Report", parameters, new DialogOptions { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true });

            }
        }

        protected async Task ConfirmDownload( string PoNumberwithLinkPo)
        {
            bool? dres = await _dialogService.ShowMessageBox("Confirmation", $"Do you want to download the PO [ {PoNumberwithLinkPo} ]?", yesText: "  Yes!  ", cancelText: "   No   ");
            if (dres == null)
            {
                return;
            }
            else
            {
                await DownloadPoAsync(PoNumberwithLinkPo);
            }
        }

        protected async Task DownloadPoAsync(string PoNumberwithLinkPo)
        {
            var POnumbers = PoNumberwithLinkPo.Split('~').ToList();
            foreach (var po in POnumbers)
            {
                try
                {
                    var (bytes, filename) = await _PODownloadService.TryGetPOPdfAsync(po.Trim());//"5900998021"
                    string base64 = Convert.ToBase64String(bytes);
                    await _jS.InvokeVoidAsync("openPOPdfInNewTab", base64, filename);

                }
                catch (Exception ex)
                {
                    _Snackbar.Add($"{ex.Message}", Severity.Error, config =>
                    {
                        config.VisibleStateDuration = 10000; // milliseconds (10 seconds)
                        config.ShowTransitionDuration = 500;
                        config.HideTransitionDuration = 500;
                    });
                }
                finally
                {
                    
                }
            }
        }

        protected void GetASNDoc(string path)
        {
            _jS.InvokeVoidAsync("open", path, "_blank");
        }
    }
}
