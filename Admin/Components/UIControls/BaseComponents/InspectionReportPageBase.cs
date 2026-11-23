using Admin.Components.Shared;
using Application.Interfaces.V1;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Admin.Components.UIControls.BaseComponents
{
    public abstract class InspectionReportPageBase : ComponentBase
    {
        [Inject] protected IAPICall _ApiMasterService { get; set; } = default!;
        [Inject] protected ISnackbar _Snackbar { get; set; } = default!;
        [Inject] protected IJSRuntime _JS { get; set; } = default!;

        [Parameter] public int InspectId { get; set; }
        [Parameter] public bool IsPreview { get; set; } = false;
        protected bool IsLoading { get; set; }
        protected InspectionReportPdfModel? _reportModel;

        protected MessageBox? messageBox;

        protected async Task LoadReportModel()
        {
            if (InspectId <= 0) return;
            try
            {
                var selectRequest = new SelectListReq
                {
                    Id = InspectId,
                    Cmd = CmdNames.Get_Inspection_Report_Pdf,
                    StrField = ""
                };
                IsLoading = true;
                var result = await _ApiMasterService.CallingAPI<InspectionQueryResult, SelectListReq>(AllApiNames.GET_INSPECTION_REPORT_VIEW!, selectRequest);
                _reportModel = new InspectionReportPdfModel(result);
                if (_reportModel is not null)
                {
                    (rpt, sup, title) = GetInspectionLabel(_reportModel.InspectionTypeId);
                }
            }
            catch (Exception ex)
            {
                _Snackbar.Add($"{ex.Message}", Severity.Error);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected async Task ExportToExcel(string exportType = "Pdf", string strField = "PASS-FAIL")
        {
            try
            {
                if (messageBox == null)
                {
                    _Snackbar.Add("Message box not initialized.", Severity.Error);
                    return;
                }
                var res = await messageBox.ShowAsync("Confirm", $"Would you like to download the {exportType} file?", MessageBox.MessageBoxButton.YesNo);
                if (res != MessageBox.MessageBoxResult.Yes) return;

                var selectRequest = new SelectListReq
                {
                    Id = InspectId,
                    Cmd = CmdNames.Get_Inspection_Report_Pdf,
                    StrField = strField
                };
                IsLoading = true;
                var result = await _ApiMasterService.CallingAPI<string, SelectListReq>(AllApiNames.GET_INSPECTION_PDF!, selectRequest);
                IsLoading = false;

                if (string.IsNullOrEmpty(result))
                {
                    _Snackbar.Add("No data available for export.", Severity.Warning);
                    return;
                }

                if (exportType == "Pdf")
                    await _JS.InvokeVoidAsync("downloadBase64Pdf", result, "InspectionReport.pdf");
                else
                    await _JS.InvokeVoidAsync("downloadFileFromBase64",
                        $"Inspections_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result);
            }
            catch (Exception ex)
            {
                _Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected (string rpt, string sup, string title) GetInspectionLabel(int? typeId)
        {
            return typeId switch
            {
                1 => ("1", "st", "Final Inspection Report"),
                2 => ("2", "nd", "Final Inspection Report"),
                3 => ("3", "rd", "Final Inspection Report"),
                4 => ("Inline", "", "Inline Inspection Report"),
                _ => ("", "", "Inspection Report")
            };
        }
        protected void OpenInNewTab(string url)
        {
            _JS.InvokeVoidAsync("open", url, "_blank");
        }

        protected string rpt = string.Empty;
        protected string sup = string.Empty;
        protected string title = string.Empty;
    }
}
