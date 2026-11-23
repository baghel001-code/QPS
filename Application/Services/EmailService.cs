using Application.Interfaces.V1;
using Application.Interfaces.V1.Notifications;
using Application.Notifications;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Entities.AppSettings;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Notifications;
using Domain.Entities.Request;
using Domain.Entities.Response;
using EmailService;
using EmailService.Models;
using Microsoft.Extensions.Logging;
using MiniMapper;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;

namespace Application.Services
{
    public class VMMEmailService : IEmailService
    {
        private readonly ILogger<VMMEmailService> _logger;
        private readonly MailSender _sender;
        private readonly IMapper _mapper;
        private readonly EmailServerSettings _emailServerSettings;
        private readonly IAPICall _aPICall;
        private readonly INotifier<DailyScheduleModel> _dailyScheduleNotifier;
        public VMMEmailService(ILogger<VMMEmailService> logger, MailSender sender, IMapper mapper, EmailServerSettings emailServerSettings, IAPICall aPICall, INotifier<DailyScheduleModel> dailyScheduleNotifier)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _emailServerSettings = emailServerSettings ?? throw new ArgumentNullException(nameof(emailServerSettings));
            _aPICall = aPICall ?? throw new ArgumentNullException(nameof(aPICall));
            _dailyScheduleNotifier = dailyScheduleNotifier;
        }

        public async Task DailyEmails()
        {
            try
            {
                //_logger.LogInformation("Creating daily email records.");
                var res = await _aPICall.CallingAPI<GenRes, string>(AllApiNames.CREATE_DAILY_EMAIL!, "");
                if (res == null)
                {
                    _logger.LogError("DailyEmails: API returned null response.");
                    throw new InvalidOperationException("CREATE_DAILY_EMAIL API returned null.");
                }
                //_logger.LogInformation("Daily email records created. ResponseCode={Code}", res.responseCode);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Error while creating daily email records.");
                throw;
            }

        }

        public async Task ProcessEmails()
        {
            try
            {
                //_logger.LogInformation("Fetching pending emails.");
                var res = await _aPICall.CallingAPI<GetAllRecipients, string>(AllApiNames.GET_PENDING_EMAILS!, "");

                // FIX #2 — Null-check API response before use
                if (res == null)
                {
                    _logger.LogError("ProcessEmails: GET_PENDING_EMAILS returned null.");
                    return;
                }

                if (res.responseCode != 0)
                {
                    _logger.LogWarning("ProcessEmails: GET_PENDING_EMAILS returned non-zero code {Code}. Skipping.", res.responseCode);
                    return;
                }
                // FIX #3 — Guard against null RecipientList before calling .Any()
                if (res.RecipientList == null || !res.RecipientList.Any())
                {
                    //_logger.LogInformation("ProcessEmails: No pending recipients. Nothing to send.");
                    return;
                }

                // Fetch inspection data and generate Excel once for all recipients
                byte[] excelBytes = await BuildExcelAttachmentAsync();

                if (excelBytes is null || excelBytes.Length == 0)
                {
                    // no data to attach — handle accordingly
                    _logger.LogWarning("No data available to export..");
                    await MarkStatus(res.RecipientList[0].ID, CmdNames.Mark_Success);
                    return;
                }
                // Send to each recipient independently — one failure must not block others
                foreach (var recipient in res.RecipientList)
                {
                    recipient.FileBytes = excelBytes;
                    await ProcessSingleRecipientAsync(recipient);
                }

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled exception occurred while processing emails.");

                throw;
            }

        }

        private async Task<byte[]> BuildExcelAttachmentAsync()
        {
            var selectListReq = new SelectListReq
            {
                Id = 0,
                StrField = "",
                Cmd = CmdNames.Get_Assigned_QAM
            };

            // _logger.LogInformation("Fetching scheduled inspection data for Excel.");
            var schedule = await _aPICall.CallingAPI<AssignPendingInspectionListResponse, SelectListReq>(AllApiNames.GET_QAMLISTTOMAIL, selectListReq);

            // FIX #2 — Null-check before accessing .Any()
            if (schedule == null || schedule.ListofInspections == null || !schedule.ListofInspections.Any())
            {
                _logger.LogWarning("No inspection data returned. Email will be sent without attachment.");
                return null;
            }

            // _logger.LogInformation("Generating Excel for {Count} inspection records.", schedule.ListofInspections.Count);

            return GenerateExcel(schedule.ListofInspections);
        }

        private async Task ProcessSingleRecipientAsync(EmailRecipient recipient)
        {
            // FIX #4 — Validate recipient email before attempting to send
            if (string.IsNullOrWhiteSpace(recipient.RECIPIENT))
            {
                _logger.LogWarning("Recipient ID={Id} has no email address. Skipping.", recipient.ID);
                return;
            }

            try
            {
                await MarkStatus(recipient.ID, CmdNames.Mark_Attempt);

                var result = await SendDailyEmailAsync(recipient);

                //if (result == null)
                //{
                //    // FIX #5 — Treat null MailResponse as a failure rather than silently succeeding
                //    throw new InvalidOperationException("SendDailyEmailAsync returned null MailResponse.");
                //}

                //if (!result.isSuccess)
                //{
                //    _logger.LogWarning("Email not delivered to {Recipient}. Reason: {Reason}", recipient.RECIPIENT, result.responseMessage);
                //    throw new Exception(result.responseMessage);
                //}
                if (!result)
                {
                    _logger.LogWarning("Email not delivered to {Recipient}.", recipient.RECIPIENT);
                    throw new Exception($"Email not delivered to {recipient.RECIPIENT}");
                }


                await MarkStatus(recipient.ID, CmdNames.Mark_Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} (ID={Id}).", recipient.RECIPIENT, recipient.ID);

                // Best-effort failure mark — don't let a MarkStatus failure mask the real error
                try
                {
                    await MarkStatus(recipient.ID, CmdNames.Mark_Failure, ex.Message);
                }
                catch (Exception markEx)
                {
                    // FIX #6 — MarkStatus failure was completely unhandled before
                    _logger.LogError(markEx, "Additionally failed to mark failure status for ID={Id}.", recipient.ID);
                }
            }
        }

        public async Task<bool> SendDailyEmailAsync(EmailRecipient schedule)
        {
            try
            {
                var sent = await _dailyScheduleNotifier.NotifyAsync(new DailyScheduleModel(schedule));
                if (sent)
                {
                    _logger.LogInformation("Email sent successfully.");
                }
                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending email to {Recipient}.", schedule.RECIPIENT);
                throw;
            }
        }

        private async Task MarkStatus(int id, string sp, string error = "")
        {
            try
            {
                var req = new SelectListReq
                {
                    Id = id,
                    Cmd = sp,
                    StrField = error
                };
                await _aPICall.CallingAPI<GenRes, SelectListReq>(AllApiNames.MARK_STATUS!, req);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark status. Id={Id}, Status={Status}", id, sp);
                throw;
            }
        }

        private byte[] GenerateExcel(List<INSPECTIONLISTMODEL> data)
        {
            // FIX #7 — Guard against null/empty data (caller already checks, but defence-in-depth)
            if (data == null || !data.Any())
            {
                _logger.LogWarning("GenerateExcel called with empty data. Returning empty byte array.");
                return Array.Empty<byte>();
            }

            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Inspection Schedule");

                // ── Headers ──────────────────────────────────────────────────
                var headers = new[]
                {
                    "Inspection Date", "Type of Inspection", "Inspection No", "Buyer",
                    "Inspector", "Article No.", "PO #", "Description", "Vendor Code",
                    "Vendor Name", "PO Qty.", "Offered Quantity in Request Form",
                    "Sample Size", "Apparels", "Category", "Location", "Address",
                    "Contact Name", "Contact #"
                };

                for (int col = 0; col < headers.Length; col++)
                    ws.Cell(1, col + 1).Value = headers[col];

                // Style header row
                var headerRow = ws.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

                // ── Data rows ────────────────────────────────────────────────
                int row = 2;
                foreach (var item in data)
                {
                    ws.Cell(row, 1).Value = item.QA_ASSIGNED_DOI.ToString("dd-MMM-yyyy");
                    ws.Cell(row, 2).Value = item.INSPECTION_TYPE;
                    ws.Cell(row, 3).Value = item.INSPECTION_NO;
                    ws.Cell(row, 4).Value = $"{item.BUYER_NAME} [{item.BUYER_ID}]";
                    ws.Cell(row, 5).Value = $"{item.EMP_NAME} [{item.INSPECTOR_EMP_CODE}]";
                    ws.Cell(row, 6).Value = item.ARTICLE;
                    ws.Cell(row, 7).Value = item.PO_NUMBER;
                    ws.Cell(row, 8).Value = item.ARTICLE_DESC;
                    ws.Cell(row, 9).Value = item.VENDOR_CODE;
                    ws.Cell(row, 10).Value = item.VENDOR_NAME;
                    ws.Cell(row, 11).Value = (int)item.TOTAL_QTY;
                    ws.Cell(row, 12).Value = (int)item.OFFERED_QTY;
                    ws.Cell(row, 13).Value = item.SAMPLE_SIZE;
                    ws.Cell(row, 14).Value = item.APPARELS;
                    ws.Cell(row, 15).Value = item.CATEGORY;
                    ws.Cell(row, 16).Value = item.REGION;
                    ws.Cell(row, 17).Value = $"{item.ADDRESS_LINE1}, {item.CITY}, PIN-{item.PINCODE}";
                    ws.Cell(row, 18).Value = item.CONTACT_PERSON;
                    ws.Cell(row, 19).Value = item.MOBILE_NUMBERS;
                    row++;
                }

                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inspection schedule excel.");

                throw;
            }

        }

    }
}
