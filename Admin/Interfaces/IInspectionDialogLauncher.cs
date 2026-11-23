using Domain.Entities.Modals;
using MudBlazor;

namespace Admin.Interfaces
{
    public interface IInspectionDialogLauncher
    {
        Task<bool> ShowAsync(IDialogService dialogService, POHeader po, bool isEditMode);
    }
    public interface IAsnDialogLauncher
    {
        Task<bool> ShowAsync(IDialogService dialogService, AsnRequestModel requestDetails);
    }
}
