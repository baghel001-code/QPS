// ================================================================
// Web / Services / WizardStateService.cs
//
// Scoped Blazor Server service — one instance per SignalR circuit.
// Holds the wizard's in-memory state between step navigations.
// Injected into AuditWizard.razor via IWizardStateService.
// ================================================================
using Application.DTOs;
using Application.Interfaces.V1;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Reflection.Emit;

namespace Application.Services.V1;

public sealed class AuditStateService : IAuditStateService
{
    private readonly IAPICall _aPICall;
    private readonly ILogger<AuditStateService> _logger;

    public WizardStateDto State { get; private set; } = new();
    public List<KeyElementResponse> Elements { get; private set; } = new();
    public List<DocumentItemResponse> DocItems { get; private set; } = new();
    public List<AttachmentItemResponse> AttachItems { get; private set; } = new();

    public event Action? StateChanged;

    public AuditStateService(IAPICall aPICall, ILogger<AuditStateService> logger)
    {
        _aPICall = aPICall;
        _logger = logger;
    }

    // ── Initialise ────────────────────────────────────────────

    public async Task InitialiseAsync(int? existingAuditId = null)
    {
        // Load master reference data once
        (Elements, DocItems, AttachItems) = await LoadMasterDataAsync();


        // Seed wizard state from master data
        State.ElementSteps = Elements.Select(e => new ElementScoresRequest
        {
            ElementId = e.ElementId,
            CheckPoints = e.CheckPoints.Select(c => new CheckPointScoreRequest
            {
                CheckPointId = c.CheckPointId
            }).ToList()
        }).ToList();

        State.Step3Docs = DocItems.Select(d => new DocumentResponseRequest
        {
            ItemId = d.ItemId
        }).ToList();

        State.Step14Attachments = AttachItems.Select(a => new AttachmentRequest
        {
            ItemId = a.ItemId,
            AttachmentPath = string.Empty
        }).ToList();

        if (existingAuditId.HasValue)
        {
            AuditBaseReq req = new(existingAuditId.Value, "");
            await HydrateFromServerAsync(req);
        }


        NotifyStateChanged();
    }

    private async Task<(List<KeyElementResponse> Elements, List<DocumentItemResponse> DocItems, List<AttachmentItemResponse> AttachItems)> LoadMasterDataAsync()
    {
        // Fire all three calls in parallel
        var (elementsTask, documentsTask, attachmentsTask) = (
            FetchMasterAsync<KeyElementResponse>(AllApiNames.GET_ALL_ELEMENTS_MASTER, "elements"),
            FetchMasterAsync<DocumentItemResponse>(AllApiNames.GET_ALL_DOCUMENTS_MASTER, "documents"),
            FetchMasterAsync<AttachmentItemResponse>(AllApiNames.GET_ALL_ATTACHMENTS_MASTER, "attachments")
        );

        await Task.WhenAll(elementsTask, documentsTask, attachmentsTask);

        return (await elementsTask, await documentsTask, await attachmentsTask);
    }

    private async Task<List<T>> FetchMasterAsync<T>(string apiName, string label)
    {
        try
        {
            var result = await _aPICall.CallingAPI<IReadOnlyList<T>, string>(apiName, "");

            if (result is null)
            {
                _logger.LogError("{ApiName} returned null response.", apiName);
                throw new InvalidOperationException($"{apiName} returned null.");
            }

            return result.ToList();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error loading {Label} master records.", label);
            throw;
        }
    }



    // ── Navigation ────────────────────────────────────────────

    public void NavigateTo(int step)
    {
        if (!State.IsStepUnlocked(step)) return;
        State.CurrentStep = step;
        NotifyStateChanged();
    }

    public void GoBack()
    {
        State.CurrentStep = Math.Max(1, State.CurrentStep - 1);
        NotifyStateChanged();
    }

    private void Advance(int next)
    {
        State.CurrentStep = next;
        State.MaxUnlockedStep = Math.Max(State.MaxUnlockedStep, next);
        NotifyStateChanged();
    }


    // ── Step Save Methods ─────────────────────────────────────
    public async Task<string?> AuditStartAsync(StartAuditRequest dto)
    {
        ApiResult<AuditSummaryResponse> result;
        try
        {
            result = await _aPICall.CallingAPI<ApiResult<AuditSummaryResponse>, StartAuditRequest>(AllApiNames.START_AUDIT!, dto);

            if (result is null)
            {
                _logger.LogError("Audit Start not responded.");
                throw new InvalidOperationException($"Audit Start returned null.");
            }
            else
            {
                if (!result.Success) return result.Errors.FirstOrDefault();

                ApplySummary(result.Data!);
               
            }
        }
        catch { }

        return null;
    }
    public async Task<string?> SaveStep1Async(StartAuditRequest dto)
    {
        ApiResult<AuditSummaryResponse> result;
        try
        {
            //if (State.AuditId == 0)
            //    result = await _aPICall.CallingAPI<ApiResult<AuditSummaryResponse>, StartAuditRequest>(AllApiNames.START_AUDIT!, dto);
            //else
            result = await _aPICall.CallingAPI<ApiResult<AuditSummaryResponse>, StartAuditRequest>(AllApiNames.SAVE_STEP_ONE!, dto);

            if (result is null)
            {
                _logger.LogError("GET_AUDIT_DETAIL returned null response.");
                throw new InvalidOperationException($"GET_AUDIT_DETAIL returned null.");
            }
            else
            {
                if (!result.Success) return result.Errors.FirstOrDefault();

                ApplySummary(result.Data!);
                State.Step1 = dto;
                Advance(2);
            }
        }
        catch { }

        return null;
    }

    public async Task<string?> SaveStep2Async(VendorInfoRequest dto)
    {
        ApiResult<AuditSummaryResponse> result;
        try
        {
            result = await _aPICall.CallingAPI<ApiResult<AuditSummaryResponse>, VendorInfoRequest>(AllApiNames.SAVE_STEP_TWO!, dto);

            if (result is null)
            {
                _logger.LogError("SAVE_STEP_TWO returned null response.");
                throw new InvalidOperationException($"SAVE_STEP_TWO returned null.");
            }
            else
            {
                if (!result.Success) return result.Errors.FirstOrDefault();
                ApplySummary(result.Data!);
                State.Step2 = dto;
                Advance(3);
            }
        }
        catch { }

        return null;
    }

    public async Task<string?> SaveStep3Async(ListOfDocumentResponseRequest docs)
    {
        ApiResult<AuditSummaryResponse> result;
        try
        {
            result = await _aPICall.CallingAPI<ApiResult<AuditSummaryResponse>, ListOfDocumentResponseRequest>(AllApiNames.SAVE_STEP_THREE!, docs);
            if (result is null)
            {
                _logger.LogError("SAVE_STEP_TWO returned null response.");
                throw new InvalidOperationException($"SAVE_STEP_TWO returned null.");
            }
            else
            {
                if (!result.Success) return result.Errors.FirstOrDefault();
                ApplySummary(result.Data!);
                State.Step3Docs = docs.documentResponseRequests;
                Advance(4);
            }
        }
        catch { }

        //NavigateTo(4);
        return null;
    }

    public async Task<string?> SaveElementStepAsync(ElementScoresRequest dto)
    {
        ApiResult<AuditSummaryResponse> result;
        try
        {
            result = await _aPICall.CallingAPI<ApiResult<AuditSummaryResponse>, ElementScoresRequest>(AllApiNames.SAVE_AUDIT_ELEMENT!, dto);
            // var result1 = await _wizardSvc.SaveElementStepAsync(dto);
            if (result is null)
            {
                _logger.LogError("SAVE_AUDIT_ELEMENT returned null response.");
                throw new InvalidOperationException($"SAVE_AUDIT_ELEMENT returned null.");
            }
            else
            {
                if (!result.Success) return result.Errors.FirstOrDefault();
                ApplySummary(result.Data!);
                var idx = State.ElementSteps.FindIndex(e => e.ElementId == dto.ElementId);
                if (idx >= 0) State.ElementSteps[idx] = dto;
                Advance(State.CurrentStep + 1);
            }
        }
        catch { }

        return null;
    }

    public async Task<string?> SaveStep14Async(ListOfAttachmentRequest attachments)
    {
        ApiResult<AuditSummaryResponse> result;
        try
        {
            result = await _aPICall.CallingAPI<ApiResult<AuditSummaryResponse>, ListOfAttachmentRequest>(AllApiNames.SAVE_AUDIT_ATTACHMENTS!, attachments);
            if (result is null)
            {
                _logger.LogError("SAVE_AUDIT_ATTACHMENTS returned null response.");
                throw new InvalidOperationException($"SAVE_AUDIT_ATTACHMENTS returned null.");
            }
            else
            {
                if (!result.Success) return result.Errors.FirstOrDefault();
                ApplySummary(result.Data!);
                State.Step14Attachments = attachments.attachmentRequests;
                Advance(15);
            }
        }
        catch
        {
            throw new InvalidOperationException($"BAD REQUEST : SAVE_AUDIT_ATTACHMENTS returned null.");
        }


        return null;
    }

    public async Task<string?> SaveStep15Async(OverallCommentsRequest dto)
    {
        ApiResult<AuditSummaryResponse> result;
        try
        {
            result = await _aPICall.CallingAPI<ApiResult<AuditSummaryResponse>, OverallCommentsRequest>(AllApiNames.SAVE_AUDIT_COMMENTS!, dto);
            if (result is null)
            {
                _logger.LogError("SAVE_AUDIT_COMMENTS returned null response.");
                throw new InvalidOperationException($"SAVE_AUDIT_COMMENTS returned null.");
            }
            else
            {
                if (!result.Success) return result.Errors.FirstOrDefault();
                ApplySummary(result.Data!);
                State.Step15 = dto;
                Advance(16);
            }
        }
        catch { }

        return null;
    }

    public async Task<string?> SubmitAuditAsync(AuditBaseReq obj)
    {
        ApiResult<ScoreResultResponse> result;
        try
        {
            result = await _aPICall.CallingAPI<ApiResult<ScoreResultResponse>, AuditBaseReq>(AllApiNames.SUBMIT_AUDIT!, obj);

            if (result is null)
            {
                _logger.LogError("SUBMIT_AUDIT returned null response.");
                throw new InvalidOperationException($"SUBMIT_AUDIT returned null.");
            }
            else
            {
                if (!result.Success) return result.Errors.FirstOrDefault();
                State.ScoreResult = result.Data;
                State.IsSubmitted = true;
                NotifyStateChanged();
            }
        }
        catch { }

        return null;
    }

    // ── Private Helpers ───────────────────────────────────────

    private async Task HydrateFromServerAsync(AuditBaseReq obj)
    {
        try
        {
            var result = await _aPICall.CallingAPI<ApiResult<AuditDetailResponse>, AuditBaseReq>(AllApiNames.GET_AUDIT_DETAIL!, obj);

            if (result is null)
            {
                _logger.LogError("GET_AUDIT_DETAIL returned null response.");
                throw new InvalidOperationException($"GET_AUDIT_DETAIL returned null.");
            }

            if (!result.Success || result.Data is null) return;

            var d = result.Data;
            State.AuditId = d.AuditId;
            State.AuditCode = d.AuditCode;
            State.StatusName = d.StatusName;
            State.CurrentStep = Math.Min(d.CurrentStep, WizardStateDto.TotalSteps);
            State.MaxUnlockedStep = State.CurrentStep;
            State.IsSubmitted = d.StatusId >= 3;

            State.Step1 = new() { AuditReqId = d.AuditReqId, AuditorEmpCode = d.AuditorEmpCode, AuditDate = d.AuditDate, AuditId = obj.AuditId, AuditorName = d.AuditorName, UpdatedBy = d.UpdatedBy };
            State.Step2 = new() { FactoryOwnerEmail = d.FactoryOwnerEmail };
            State.Step15.OverallComments = d.OverallComments;

            if (d.AuditScorePercent.HasValue && d.GradeCode.HasValue)
            {
                State.ScoreResult = new ScoreResultResponse(
                    AuditId: d.AuditId,
                    AuditCode: d.AuditCode,
                    ScorePercent: d.AuditScorePercent.Value,
                    GradeCode: d.GradeCode.Value,
                    ColorHex: d.GradeColorHex ?? string.Empty,
                    Description: d.Description ?? string.Empty,
                    ValidationPeriod: d.ValidationPeriod ?? string.Empty,
                    ValidityTill: d.ValidityTill ?? default,
                    IsBlacklistRisk: d.IsBlacklistRisk
                );
            }
            else
            {
                State.ScoreResult = null; // audit hasn't been scored yet
            }

            if (d.DocumentResponses.Any())
                State.Step3Docs = d.DocumentResponses.Select(r => new DocumentResponseRequest
                {
                    ItemId = r.ItemId,
                    IsAvailable = r.IsAvailable,
                    ValidityDate = r.ValidityDate,
                    UnitInfo = r.UnitInfo,
                    Attachments = r.Attachments,
                    Remarks = r.Remarks
                }).ToList();

            foreach (var elem in State.ElementSteps)
                foreach (var cp in elem.CheckPoints)
                {
                    var saved = d.CheckPointResponses.FirstOrDefault(r => r.CheckPointId == cp.CheckPointId);
                    if (saved is not null) { cp.Score = saved.Score; cp.Remarks = saved.Remarks; }
                }

            if (d.AttachmentResponses.Any())
                State.Step14Attachments = d.AttachmentResponses.Select(r => new AttachmentRequest
                {
                    ItemId = r.ItemId,
                    AttachmentPath = r.Attachment ?? string.Empty,
                    Remarks = r.Remarks ?? string.Empty,

                }).ToList();

            if (d.RatingGridResponse.Any())
            {
                State.RatingGridList = d.RatingGridResponse.Select(x =>
                    new AuditGradeResponse(
                    GradeId: x.GRADE_ID,
                    GradeCode: x.GRADE_CODE.FirstOrDefault(),
                    MinScore: x.MIN_SCORE,
                    MaxScore: x.MAX_SCORE,
                    ValidityInMonths: x.VALIDITY_IN_MONTHS,
                    ValidationPeriod: x.VALIDATION_PERIOD,
                    ColorHex: x.COLOR_HEX,
                    Description: x.DESCRIPTION
                )).ToList();
            }
            else
            {
                State.RatingGridList = null; // audit hasn't been scored yet
            }

        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error GET_AUDIT_DETAIL data.");
            throw;
        }


    }

    private void ApplySummary(AuditSummaryResponse s)
    {
        State.AuditId = s.AuditId;
        State.AuditCode = s.AuditCode;
        State.StatusName = s.StatusName;
    }

    // ── Save current step (delegates to wizard service) ──────

    private void NotifyStateChanged() => StateChanged?.Invoke();
}