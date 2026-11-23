namespace Admin.Components.UIControls.BaseComponents
{
    using Admin.Components.Shared;
    using Admin.Services;
    using Application.Interfaces.V1;
    using Domain.Entities.Common;
    using Domain.Entities.Modals;
    using Domain.Records;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using MiniMapper;
    using MudBlazor;

    public abstract class InspectionListPageBase : QpsPageBase
    {
        [Inject] protected IAPICall _ApiMasterService { get; set; } = default!;
        [Inject] protected IJSRuntime _JS { get; set; } = default!;
        [Inject] protected IMapper _mapper { get; set; } = default!;
        [Inject] protected IDialogService _DialogService { get; set; } = default!;
        [Inject] protected IInspectionDetailLoader _DetailLoader { get; set; } = default!;
        [Inject] protected InspectionDialogLauncherFactory _LauncherFactory { get; set; } = default!;


        protected int _totalCount;
        protected InspectionSearchFilterMultiSelection _filter = new();
        protected MessageBox? messageBox;
        protected List<INSPECTION_REQUEST_LIST_MODEL> filteredPO = new();
        protected MudTable<INSPECTION_REQUEST_LIST_MODEL> mudTable = default!;
        protected bool IsLoading = false;

        // Override this per-page so OnAfterRenderAsync picks the right Cmd
        protected abstract Dictionary<string, string> RoleCmdMap { get; }
        protected abstract string ListTypeCmd { get; }
        protected virtual string SubType => "ALL";

        protected async Task OnPageChanged(int newPage)
        {
            _filter.PageNumber = newPage;
            await GetPOs(_filter);
        }

        protected async Task OnPageSizeChanged(int newSize)
        {
            _filter.PageSize = newSize;
            _filter.PageNumber = 1;
            await GetPOs(_filter);
        }

        protected async Task HandleSearchMultiSelect(InspectionSearchFilterMultiSelection filter)
        {
            _filter.PageNumber = 1;
            await GetPOs(_filter);
        }

        protected override Task OnUserInitializedAsync()
        {
            _filter.StrField = _CurrentUser.UserCode;
            return Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            try
            {
                string? roleKey = (_CurrentUser.IsQAM || _CurrentUser.IsQAAdmin || _CurrentUser.IsMerchandiser) ? "Admin" :
                                  (_CurrentUser.IsBuyer || _CurrentUser.IsACH || _CurrentUser.IsCH) ? "Buyer" :
                                   _CurrentUser.IsQA ? "QA" :
                                   _CurrentUser.IsVendor ? "Vendor" :
                                   null;

                if (roleKey == null || !RoleCmdMap.TryGetValue(roleKey, out var cmd))
                {
                    _Snackbar.Add("User role not recognized.", Severity.Warning);
                    return;
                }

                _filter.Cmd = cmd;
                _filter.SubType = SubType;

                await GetPOs(_filter);

            }
            catch (Exception ex)
            {
                _Snackbar.Add($"Error loading inspections: {ex.Message}", Severity.Error);
            }
            finally
            {
                StateHasChanged();
            }
        }

        protected async Task GetPOs(InspectionSearchFilterMultiSelection filterReq)
        {
            try
            {
                filteredPO = new List<INSPECTION_REQUEST_LIST_MODEL>();
                IsLoading = true;
                StateHasChanged();
                var res = await _ApiMasterService.CallingAPI<ReceivedPendingOngoingInspections, InspectionSearchFilterMultiSelection>(
                    AllApiNames.PENDING_RECEIVED_ONGOING_INSPECTIONS, filterReq);


                if (res == null)
                {
                    _Snackbar.Add("No response from server. Please try again.", Severity.Error);
                    return;
                }

                if (res.responseCode == 0)
                {
                    if (res.InspectionList == null || !res.InspectionList.Any())
                    {
                        _Snackbar.Add("No records found.", Severity.Info);
                        _totalCount = 0;
                    }
                    else
                    {
                        filteredPO = res.InspectionList;
                        _totalCount = res.TotalRows;
                    }
                }
                else
                {
                    _Snackbar.Add(res.responseMessage ?? "An error occurred.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
                _Snackbar.Add($"Error loading PO list: {ex.Message}", Severity.Error);
                filteredPO = new List<INSPECTION_REQUEST_LIST_MODEL>();
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected async Task UpdateLinkPO(INSPECTION_REQUEST_LIST_MODEL? PO = null)
        {
            if (PO == null) return;

            var parameters = new DialogParameters { { "PO", PO } };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Large, FullWidth = true };

            var dialog = await _DialogService.ShowAsync<UpdateNewLinkPO>("Link New PO Number", parameters, options);
            var result = await dialog.Result;
            if (!result!.Canceled)
            {
                _filter.Reset();
                await GetPOs(_filter);
            }
        }

        protected async Task UpdateInspectionDialog(INSPECTION_REQUEST_LIST_MODEL? modelObj = null)
        {
            var po = modelObj != null
                ? await _DetailLoader.LoadAsync(new InspectionDetailRequest(modelObj.INSP_ID, UseQamRegionList: true))
                : new POHeader();

            if (_CurrentUser.IsQAAdmin || _CurrentUser.IsQAM || _CurrentUser.IsQA)
            {
                var refresh = await new QaInspectionDialogLauncher().ShowAsync(_DialogService, po!, po != null);
                if (refresh) { _filter.Reset(); await GetPOs(_filter); }
            }
        }

        protected async Task ASNRequestDialog(INSPECTION_REQUEST_LIST_MODEL? modelObj = null)
        {
            AsnRequestModel requestDetails = new();
            if (modelObj != null)
            {
                requestDetails.PoNumber = modelObj.PO_NUMBER;
                requestDetails.InspectionNo = modelObj.INSPECTION_REPORT_NO;
                requestDetails.TotalOrderQty = modelObj.PO_QTY;
                requestDetails.ShipQty = decimal.TryParse(modelObj.SHIP_QTY, out var shipQty) ? shipQty : 0m;
                requestDetails.InspId = modelObj.INSP_ID;
                requestDetails.InspectId = modelObj.INSPECT_ID;
            }

            var currentRole = new UserRoleContext
            {
                IsBFT = _CurrentUser.IsBFT,
                IsBuyer = _CurrentUser.IsBuyer,
                IsQAAdmin = _CurrentUser.IsQAAdmin
            };



            var launcher = _LauncherFactory.ResolveAsn(currentRole);
            var refresh = await launcher.ShowAsync(_DialogService, requestDetails);
            //if (refresh) await GetPOs(_filter);
        }
       
        protected async Task ViewReport(INSPECTION_REQUEST_LIST_MODEL? modelObj = null)
        {
           
            if (modelObj != null)
            {
                if (modelObj.STATUS_CODE == 7 || modelObj.STATUS_CODE == 10)
                {
                    var parameters = new DialogParameters
                    {
                        ["InspectId"] = modelObj!.INSPECT_ID,
                        ["IsPreview"] = false
                    };

                    if (modelObj.INSPECTION_RESULT == 4)
                    {
                        await _DialogService.ShowAsync<InspectionReportAbortiveView>("Abortive Report Preview", parameters, new DialogOptions { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true });
                    }
                    else
                    {
                        await _DialogService.ShowAsync<InspectionReportView>("Final Inspection Report", parameters, new DialogOptions { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true });
                    }
                }

                else
                    _Snackbar.Add("Inspection is still pending.", Severity.Error);
            }
        }

        protected async Task UpdateQADOIDialog(INSPECTION_REQUEST_LIST_MODEL? PO = null)
        {
            if (PO == null) return;

            var parameters = new DialogParameters
            {
                ["IDs"] = PO.INSP_ID.ToString(),
                ["RegionIDs"] = PO.REGION_ID.ToString()
            };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge, FullWidth = true };

            var dialog = await _DialogService.ShowAsync<AssignInspectorDialog>("Assign DOI & Inspector", parameters, options);
            var result = await dialog.Result;
            if (!result!.Canceled)
            {
                _filter.Reset();
                await GetPOs(_filter);
            }
        }

        protected async Task ExportToExcel(string ExportType = "excel")
        {
            try
            {
                if (messageBox == null)
                {
                    _Snackbar.Add("Message box not initialized.", Severity.Error);
                    return;
                }

                string message = $"Would you like to download the {ExportType} file?";
                var res = await messageBox.ShowAsync("Confirm", message, MessageBox.MessageBoxButton.YesNo);
                if (res != MessageBox.MessageBoxResult.Yes) return;

                var reqObject = _filter.Clone();
                reqObject.RequestType = $"REPORT | {ListTypeCmd}";
                reqObject.IsVendor = _CurrentUser.IsVendor;
                IsLoading = true;
                StateHasChanged();

                var result = await _ApiMasterService.CallingAPI<string, InspectionSearchFilterMultiSelection>(
                    AllApiNames.EXPORT_EXCEL_PENDING_RECEIVED_ONGOING_INSPECTIONS!, reqObject);

                if (string.IsNullOrEmpty(result))
                {
                    _Snackbar.Add("No data available for export.", Severity.Warning);
                    return;
                }

                if (ExportType == "Pdf")
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


    }
}
