using Application.Interfaces.V1;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Records;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Admin.Components.UIControls.BaseComponents
{
    public abstract class DashboardBase : InspectionListPageBase
    {

        protected List<DashboardDateSummary> dashboardDateSummary = new();
        protected bool IsDashboardRole => _CurrentUser.IsQAM || _CurrentUser.IsQAAdmin;
        protected string DoI = "QAM DOI";
        protected string QA = "QA";
        private string _dashboardType => SubType == "RECEIVED" ? "" : $"_{SubType}";
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

                if (IsDashboardRole)
                {
                    await GetDashboardData();
                    StateHasChanged();
                }
                else
                {
                    await GetPOs(_filter);
                    StateHasChanged();
                }


            }
            catch (Exception ex)
            {
                _Snackbar.Add($"Error loading inspections: {ex.Message}", Severity.Error);
            }
            finally
            {
                //StateHasChanged();
            }
        }
        private async Task GetDashboardData()
        {
            SelectListReq selectListReq = new();
            selectListReq.Id = 0;
            selectListReq.StrField =_CurrentUser.UserCode;
            selectListReq.Cmd = $"QA_DASHBOARD{_dashboardType}";

            bool isSubmittedPage = string.IsNullOrEmpty(_dashboardType);

            IsLoading = true;
            var res = await _ApiMasterService.CallingAPI<DashboardModelResponse, SelectListReq>(AllApiNames.GET_DASHBOARD_DATA, selectListReq);
            IsLoading = false;

            if (res.responseCode == 0)
            {
                if (res.ListofInspections == null)
                {
                    _Snackbar.Add("No record found.", Severity.Error);
                }
                else
                {
                    var today = DateTime.Today;

                    dashboardDateSummary = res.ListofInspections
                        .Where(r => _CurrentUser.QAMAdminRegions.Contains(r.REGION_ID.ToString()))
                        .GroupBy(x =>
                            isSubmittedPage
                                ? x.DOI.Date // Submitted: always day-wise, no month bucketing
                                : (x.DOI.Year == today.Year && x.DOI.Month == today.Month)
                                    ? x.DOI.Date // current month → day-wise
                                    : new DateTime(x.DOI.Year, x.DOI.Month, 1) // past → month-wise
                        )
                        .Select(g => new DashboardDateSummary
                        {
                            DOI = g.Key,
                            // 1. Total
                            TotalRequests = g.Count(),
                            // 2. QA → DOI assignment status
                            QAMDOIPending = g.Count(x => !x.IS_DATE_ASSIGNED_BY_QA),
                            QAMDOIAssigned = g.Count(x => x.IS_DATE_ASSIGNED_BY_QA),
                            // 3. Inspector assignment status
                            Pending = g.Count(x => !x.IS_INSPECTOR_ASSIGNED),
                            Assigned = g.Count(x => x.IS_INSPECTOR_ASSIGNED),
                            // 4. Inspection IDs
                            INSP_IDS = g
                                .Where(x => x.INSP_ID > 0)
                                .Select(x => x.INSP_ID)
                                .Distinct()
                                .ToList(),
                            // 5. Pending DOI
                            View_Pending_DOI_INSP_IDS = g
                                .Where(x => x.INSP_ID > 0 && !x.IS_DATE_ASSIGNED_BY_QA)
                                .Select(x => x.INSP_ID)
                                .Distinct()
                                .ToList(),
                            // 6. Assigned DOI
                            View_Assigned_DOI_INSP_IDS = g
                                .Where(x => x.INSP_ID > 0 && x.IS_DATE_ASSIGNED_BY_QA)
                                .Select(x => x.INSP_ID)
                                .Distinct()
                                .ToList(),
                            // 7. Pending Inspector
                            View_Pendiing_INSP_IDS = g
                                .Where(x => x.INSP_ID > 0 && !x.IS_INSPECTOR_ASSIGNED)
                                .Select(x => x.INSP_ID)
                                .Distinct()
                                .ToList(),
                            // 8. Assigned Inspector
                            View_Assigned_INSP_IDS = g
                                .Where(x => x.INSP_ID > 0 && x.IS_INSPECTOR_ASSIGNED)
                                .Select(x => x.INSP_ID)
                                .Distinct()
                                .ToList(),
                            // 9. Region IDs
                            REGION_IDS = g
                                .Where(x => x.INSP_ID > 0)
                                .Select(x => x.REGION_ID)
                                .Distinct()
                                .ToList(),
                        })
                        // Submitted: oldest-first. Ongoing/Pending: newest-first.
                        .OrderBy(x => isSubmittedPage ? 0 : 1)
                        .ThenBy(x => isSubmittedPage ? x.DOI : DateTime.MinValue)
                        .ThenByDescending(x => isSubmittedPage ? DateTime.MinValue : x.DOI)
                        .ToList();

                    if (isSubmittedPage)
                    {
                        // Submitted: exact-date join only (original LINQ join semantics preserved as a lookup loop)
                        foreach (var item in dashboardDateSummary)
                        {
                            var freeze = res.ListofFreezeQA.FirstOrDefault(b => b.INSPECTION_DATE.Date == item.DOI.Date);

                            if (freeze != null)
                            {
                                item.IsFreeze = true;
                                item.FinalizeBy = $"{freeze.EMP_NAME}({freeze.EMP_CODE}) ";
                                item.FreezeOn = freeze.CREATED_DATE;
                            }
                        }
                    }
                    else
                    {
                        // Ongoing / Pending: exact date match for current month, month+year match for past
                        foreach (var item in dashboardDateSummary)
                        {
                            var freeze = res.ListofFreezeQA.FirstOrDefault(b =>
                                (item.DOI.Month == DateTime.Today.Month && item.DOI.Year == DateTime.Today.Year)
                                    ? b.INSPECTION_DATE.Date == item.DOI.Date
                                    : b.INSPECTION_DATE.Month == item.DOI.Month && b.INSPECTION_DATE.Year == item.DOI.Year
                            );

                            if (freeze != null)
                            {
                                item.IsFreeze = true;
                                item.FinalizeBy = $"{freeze.EMP_NAME}({freeze.EMP_CODE})";
                                item.FreezeOn = freeze.CREATED_DATE;
                            }
                        }
                    }
                }
            }
            else
            {
                _Snackbar.Add(res.responseMessage!, Severity.Error);
            }
        }
        protected virtual async Task OpenAssignScreen(string InspIds, string RegionIds, string dialogTitle = "Assign DOI & Inspector")
        {
            var parameters = new DialogParameters { ["IDs"] = InspIds, ["RegionIDs"] = RegionIds };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge, FullWidth = true };

            var dialog = await _DialogService.ShowAsync<AssignInspectorDialog>(dialogTitle, parameters, options);
            var result = await dialog.Result;
            if (result != null && !result.Canceled)
                await GetDashboardData();
        }

        protected virtual bool CanViewDashboardDetail(DashboardDateSummary item) => true;

        protected virtual async Task ViewAssignScreen(string InspIds, string Caption, DashboardDateSummary? item = null)
        {
            if (item != null && !CanViewDashboardDetail(item))
            {
                await _DialogService.ShowMessageBox(
                    title: "Permission Required",
                    markupMessage: (MarkupString)"This information is restricted. Please contact your administrator for access.",
                    yesText: "OK");
                return;
            }

            var parameters = new DialogParameters { ["IDs"] = InspIds };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Large, FullWidth = true };
            var dialog = await _DialogService.ShowAsync<ViewIRDetails>($"{Caption} Assigned : Inspection Request Details", parameters, options);
            var result = await dialog.Result;
            if (result != null && !result.Canceled)
                await GetDashboardData();
        }

        protected virtual async Task ViewPendingScreen(string InspIds, string Caption)
        {
            if (string.IsNullOrEmpty(InspIds)) return;

            var parameters = new DialogParameters { ["IDs"] = InspIds };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Large, FullWidth = true };
            var dialog = await _DialogService.ShowAsync<ViewIRDetails>($"{Caption} Pending : Inspection Request Details", parameters, options);
            var result = await dialog.Result;
            if (result != null && !result.Canceled)
                await GetDashboardData();
        }

        protected virtual async Task FreezeDoI(DashboardDateSummary item)
        {
            bool? dres = await _DialogService.ShowMessageBox("Confirm Finalization", "Once finalized, QAM will no longer be able to modify the inspection details. Do you want to proceed?", yesText: "  Yes!  ", cancelText: "   No   ");
            if (dres == null)
            {
                return;
            }
            else
            {
                AssignInspectorFreezeRequest reqObj = new AssignInspectorFreezeRequest()
                {
                    IsFreeze = true,
                    CreatedBy =_CurrentUser.UserId,
                    QA_ASSIGNED_DOI = item.DOI,
                    Details = ""

                };

                var res1 = await _ApiMasterService.CallingAPI<GenRes, AssignInspectorFreezeRequest>(AllApiNames.ASSIGN_INSPECTOR_FREEZE!, reqObj);
                if (res1.responseCode.Equals(0))
                {
                    _Snackbar.Add("Inspector Assigned freez successfully.", Severity.Success);
                    await GetDashboardData();
                }
                else
                {
                    await _DialogService.ShowMessageBox("Error!", res1.responseMessage!);
                }

            }

        }

        protected virtual async Task OpenRoleDialog(INSPECTION_REQUEST_LIST_MODEL? modelObj = null)
        {
            POHeader? po = null;
            if (modelObj != null)
            {
                IsLoading = true; StateHasChanged();
                po = await _DetailLoader.LoadAsync(new InspectionDetailRequest(modelObj.INSP_ID, UseQamRegionList: true, FilterInspectionTypeByPoNumber: !_CurrentUser.IsQA));
                IsLoading = false; StateHasChanged();
            }

            var currentRole = new UserRoleContext
            {
                IsQA = _CurrentUser.IsQA,
                IsQAAdmin = _CurrentUser.IsQAAdmin,
                IsQAM = _CurrentUser.IsQAM,
                IsVendor = _CurrentUser.IsVendor
            };

            var launcher = _LauncherFactory.Resolve(currentRole);
            var refresh = await launcher.ShowAsync(_DialogService, po ?? new POHeader(), po != null);
            if (refresh) await GetPOs(_filter);
        }
    }
}
