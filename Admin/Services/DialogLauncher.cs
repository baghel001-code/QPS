using Admin.Components.UIControls;
using Admin.Components.UIControls.ASN;
using Admin.Interfaces;
using Domain.Entities.Modals;
using MudBlazor;

namespace Admin.Services
{

    public class QaInspectionDialogLauncher : IInspectionDialogLauncher
    {
        public async Task<bool> ShowAsync(IDialogService dialogService, POHeader po, bool isEditMode)
        {
            var parameters = new DialogParameters { { "PO", po }, { "IsEditMode", isEditMode } };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraExtraLarge, FullWidth = true };
            var dialog = await dialogService.ShowAsync<InspectionForm>("Inspection Form", parameters, options);
            var result = await dialog.Result;
            return result is { Canceled: false };
        }
    }

    public class VendorInspectionDialogLauncher : IInspectionDialogLauncher
    {

        public async Task<bool> ShowAsync(IDialogService dialogService, POHeader po, bool isEditMode)
        {
            var parameters = new DialogParameters { { "PO", po }, { "IsEditMode", isEditMode } };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraExtraLarge, FullWidth = true };

            var dialog = await dialogService.ShowAsync<AddEditInspections>("Inspection Request", parameters, options);
            var result = await dialog.Result;
            return result is { Canceled: false };
        }
    }

    public class ASNDialogLauncher : IAsnDialogLauncher
    {
        public async Task<bool> ShowAsync(IDialogService dialogService, AsnRequestModel RequestDetails)
        {

            var parameters = new DialogParameters { { "_model", RequestDetails } };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

            var dialog = await dialogService.ShowAsync<UpdateAsn>("Update ASN", parameters, options);
            var result = await dialog.Result;
            return result is { Canceled: false };
        }

    }
    public class BFTDialogLauncher : IAsnDialogLauncher
    {
        public async Task<bool> ShowAsync(IDialogService dialogService, AsnRequestModel RequestDetails)
        {
            
            var parameters = new DialogParameters { { "_model", RequestDetails } };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

            var dialog = await dialogService.ShowAsync<AsnRequest>("ASN Request Form", parameters, options);
            var result = await dialog.Result;
            return result is { Canceled: false };
        }

    }
}
